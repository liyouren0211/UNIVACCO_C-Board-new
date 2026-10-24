// Native C# font alignment. Combined from the three original C# modules.
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

// ===== GoldenCharacters.cs =====
namespace StandardOPage
{

    public static class FontRoiBoxDebugSettings
    {
        // 只影響額外輸出圖片，不改變對位與瑕疵判斷。
        public const bool Enabled = true;
        // 與目前呼叫 BandMatch 時使用的 yRadius = 12 一致。
        public const int SearchYRadius = 12;
    }

    // ================================================================
    // 字體 Closing 設定：只需要修改這裡，不必動下面的演算法。
    // ================================================================
    public static class FontClosingSettings
    {
        // 指定要使用 Closing 的字級，可自由增刪，例如 { 9, 10, 11, 12 }。
        // 設為空集合 new HashSet<int>() 即全部不做 Closing。
        public static readonly HashSet<int> EnabledFontPts = new HashSet<int> { 7, 8, 9, 10, 11, 12 };

        // 必須是奇數且 >= 1，例如 3、5、7；1 代表無實際形態學變化。
        public const int KernelSize = 5;

        // 全域總開關。原有環境變數 UNIVACCO_FONT_A_CLOSING=0 仍可覆寫為關閉。
        public const bool Enabled = true;

        public static bool ShouldApply(int fontPt)
        {
            string env = Environment.GetEnvironmentVariable("UNIVACCO_FONT_A_CLOSING");
            bool disabledByEnvironment = string.Equals(env, "0", StringComparison.OrdinalIgnoreCase)
                || string.Equals(env, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(env, "off", StringComparison.OrdinalIgnoreCase);
            return Enabled && !disabledByEnvironment && EnabledFontPts.Contains(fontPt);
        }
    }

    // Reads exactly the BMP/CSV Golden Sample format supplied by the user; no legacy template scaling.
    public static class GoldenCharacters
    {
        public sealed class Glyph
        {
            public string Side, File;
            public int FontPt, Index, SourceWidth, SourceHeight, X0, Y0, X1, Y1;
        }
        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        // References are immutable after construction. Clear by restarting the app if golden BMP/CSV changes.
        private static readonly object ReferenceCacheLock = new object();
        private static readonly Dictionary<string, (byte[,] reference, Dictionary<int, int> xoffsets, List<Glyph> glyphs)> ReferenceCache
            = new Dictionary<string, (byte[,] reference, Dictionary<int, int> xoffsets, List<Glyph> glyphs)>(StringComparer.OrdinalIgnoreCase);
        private static List<string> CsvFields(string line)
        {
            var result = new List<string>(); var s = new System.Text.StringBuilder(); bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { s.Append('"'); i++; } else quoted = !quoted; }
                else if (c == ',' && !quoted) { result.Add(s.ToString()); s.Clear(); }
                else s.Append(c);
            }
            result.Add(s.ToString()); return result;
        }
        public static List<Glyph> ReadCsv(string directory)
        {
            string[] lines = File.ReadAllLines(Path.Combine(directory, "character_coordinates.csv"), System.Text.Encoding.UTF8);
            if (lines.Length < 2) throw new InvalidDataException("Golden Sample CSV is empty");
            string[] headers = CsvFields(lines[0].TrimStart('\ufeff')).ToArray();
            var rows = new List<Glyph>();
            for (int line = 1; line < lines.Length; line++)
            {
                if (String.IsNullOrWhiteSpace(lines[line])) continue;
                var data = CsvFields(lines[line]);
                Func<string, string> get = key => { int k = Array.IndexOf(headers, key); if (k < 0 || k >= data.Count) throw new InvalidDataException("Missing CSV column: " + key); return data[k]; };
                Func<string, int> integer = key => int.Parse(get(key), CI);
                rows.Add(new Glyph
                {
                    Side = get("side"),
                    File = get("file"),
                    FontPt = integer("font_pt"),
                    Index = integer("index_top_to_bottom"),
                    SourceWidth = integer("source_width"),
                    SourceHeight = integer("source_height"),
                    X0 = integer("x0"),
                    Y0 = integer("y0"),
                    X1 = integer("x1"),
                    Y1 = integer("y1")
                });
            }
            return rows;
        }
        public static (byte[,] reference, Dictionary<int, int> xoffsets, List<Glyph> glyphs) Load(string directory, string side)
        {
            string cacheKey = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + "|" + side;
            lock (ReferenceCacheLock)
            {
                (byte[,] reference, Dictionary<int, int> xoffsets, List<Glyph> glyphs) cached;
                if (ReferenceCache.TryGetValue(cacheKey, out cached)) return cached;
            }
            var rows = ReadCsv(directory).Where(r => r.Side == side).ToList();
            var fonts = new List<byte[,]>(); var offsets = new Dictionary<int, int>(); int totalWidth = 0, height = -1;
            for (int pt = 3; pt <= 12; pt++)
            {
                var rr = rows.Where(r => r.FontPt == pt).ToList();
                if (rr.Count == 0) throw new InvalidDataException(side + " " + pt + "pt missing");
                if (rr.Any(r => r.SourceWidth != rr[0].SourceWidth || r.SourceHeight != rr[0].SourceHeight))
                    throw new InvalidDataException("Inconsistent source dimensions");
                int w = rr[0].SourceWidth, h = rr[0].SourceHeight;
                if (height >= 0 && h != height) throw new InvalidDataException("Inconsistent font strip heights");
                height = h; byte bg = side == "Yin" ? (byte)0 : (byte)255;
                var canvas = new byte[h, w];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) canvas[y, x] = bg;
                foreach (var r in rr)
                {
                    string fn = Path.Combine(directory, r.File.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fn)) throw new FileNotFoundException("Golden BMP missing", fn);
                    using (Mat bmp = CvInvoke.Imread(fn, ImreadModes.Grayscale))
                    {
                        byte[,] pixels = CharacterBandMatcher.ReadGray(bmp);
                        if (r.X0 < 0 || r.Y0 < 0 || r.X1 > w || r.Y1 > h || pixels.GetLength(1) != r.X1 - r.X0 || pixels.GetLength(0) != r.Y1 - r.Y0)
                            throw new InvalidDataException("Glyph mismatch: " + fn);
                        for (int y = 0; y < pixels.GetLength(0); y++) for (int x = 0; x < pixels.GetLength(1); x++) canvas[r.Y0 + y, r.X0 + x] = pixels[y, x];
                    }
                }
                offsets[pt] = totalWidth; totalWidth += w; fonts.Add(canvas);
            }
            if (totalWidth != 3550 && totalWidth != 3600) throw new InvalidDataException("Unsupported Golden width " + totalWidth);
            byte[,] reference = new byte[height, totalWidth];
            int offset = 0; foreach (var font in fonts)
            {
                int fw = font.GetLength(1);
                for (int y = 0; y < height; y++) for (int x = 0; x < fw; x++) reference[y, offset + x] = font[y, x];
                offset += fw;
            }
            var loaded = (reference: reference, xoffsets: offsets, glyphs: rows);
            lock (ReferenceCacheLock)
            {
                if (!ReferenceCache.ContainsKey(cacheKey)) ReferenceCache[cacheKey] = loaded;
                return ReferenceCache[cacheKey];
            }
        }
        public static byte[,] Binary(byte[,] gray, bool isYin, int threshold = 150)
        {
            int h = gray.GetLength(0), w = gray.GetLength(1); var result = new byte[h, w];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    result[y, x] = (gray[y, x] > threshold) == isYin ? (byte)255 : (byte)0;
            return result;
        }
    }
}

// ===== CharacterBandMatcher.cs =====
// Native C# translation of the Python band_match scoring/search/acceptance logic.
// No Python process, runtime, script or bridge is used.
// Target: .NET Framework 4.7.2 + existing Emgu.CV project.
namespace StandardOPage
{
    public static class CharacterBandMatcher
    {
        public sealed class Match
        {
            public int BestDx, BestDy, UsedDx, UsedDy, SearchRadiusUsed;
            public bool Accepted;
            public double Before, Score;
            public string Reason;
            public bool ClosingUsedForAlignment;
            public int ClosingKernelSize;
            public Rectangle GoldenRoi;
            public byte[,] Golden, Original, Best, Used;
        }
        private sealed class Candidate
        {
            public int Dx, Dy;
            public double Score, Penalized;
        }
        public static byte[,] ReadGray(Mat mat)
        {
            using (var im = mat.ToImage<Gray, byte>())
            {
                byte[,] a = new byte[im.Height, im.Width];
                for (int y = 0; y < im.Height; y++) for (int x = 0; x < im.Width; x++) a[y, x] = im.Data[y, x, 0];
                return a;
            }
        }
        public static Mat WriteGray(byte[,] pixels)
        {
            int h = pixels.GetLength(0), w = pixels.GetLength(1);
            using (var im = new Image<Gray, byte>(w, h))
            {
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) im.Data[y, x, 0] = pixels[y, x];
                return im.Mat.Clone();
            }
        }

        public static Mat BuildFinalOverlay(byte[,] golden, byte[,] aligned)
        {
            int h = Math.Min(golden.GetLength(0), aligned.GetLength(0));
            int w = Math.Min(golden.GetLength(1), aligned.GetLength(1));
            using (var im = new Image<Bgr, byte>(w, h))
            {
                im.SetZero();
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        bool g = golden[y, x] > 0;
                        bool a = aligned[y, x] > 0;
                        if (g && a)
                        {
                            im.Data[y, x, 1] = 255; // G
                            im.Data[y, x, 2] = 255; // R => yellow
                        }
                        else if (g)
                        {
                            im.Data[y, x, 2] = 255; // red = golden
                        }
                        else if (a)
                        {
                            im.Data[y, x, 1] = 255; // green = aligned
                        }
                    }
                return im.Mat.Clone();
            }
        }

        public static Mat BuildSkeletonOverlay(byte[,] golden, byte[,] aligned)
        {
            bool[,] goldenSk = Skeleton(golden);
            bool[,] alignedSk = Skeleton(aligned);
            int h = Math.Min(goldenSk.GetLength(0), alignedSk.GetLength(0));
            int w = Math.Min(goldenSk.GetLength(1), alignedSk.GetLength(1));
            using (var im = new Image<Bgr, byte>(w, h))
            {
                im.SetZero();
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        bool g = goldenSk[y, x];
                        bool a = alignedSk[y, x];
                        if (g && a)
                        {
                            im.Data[y, x, 1] = 255;
                            im.Data[y, x, 2] = 255;
                        }
                        else if (g)
                        {
                            im.Data[y, x, 2] = 255;
                        }
                        else if (a)
                        {
                            im.Data[y, x, 1] = 255;
                        }
                    }
                return im.Mat.Clone();
            }
        }
        public static byte[,] Slice(byte[,] src, int x, int y, int w, int h)
        {
            byte[,] dst = new byte[h, w]; int sh = src.GetLength(0), sw = src.GetLength(1);
            for (int j = 0; j < h; j++) if (y + j >= 0 && y + j < sh)
                    for (int i = 0; i < w; i++) if (x + i >= 0 && x + i < sw) dst[j, i] = src[y + j, x + i];
            return dst;
        }
        // 8-connected components smaller than four pixels are removed, as in Python skeleton().
        internal static bool[,] Skeleton(byte[,] src)
        {
            int h = src.GetLength(0), w = src.GetLength(1);
            bool[,] a = new bool[h, w], visited = new bool[h, w];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[y, x] = src[y, x] > 0;
            int[] xx = { -1, 0, 1, -1, 1, -1, 0, 1 }, yy = { -1, -1, -1, 0, 0, 1, 1, 1 };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (a[y, x] && !visited[y, x])
                    {
                        var q = new Queue<Point>(); var component = new List<Point>(); q.Enqueue(new Point(x, y)); visited[y, x] = true;
                        while (q.Count > 0)
                        {
                            Point p = q.Dequeue(); component.Add(p);
                            for (int k = 0; k < 8; k++)
                            {
                                int nx = p.X + xx[k], ny = p.Y + yy[k];
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h && a[ny, nx] && !visited[ny, nx])
                                { visited[ny, nx] = true; q.Enqueue(new Point(nx, ny)); }
                            }
                        }
                        if (component.Count < 4) foreach (Point p in component) a[p.Y, p.X] = false;
                    }
            // Zhang-Suen two-subiteration thinning, the same algorithm family as skimage's 2D skeletonize.
            // Pixel-level equivalence requires regression verification on the provided real images.
            bool changed;
            do
            {
                changed = false;
                for (int pass = 0; pass < 2; pass++)
                {
                    var remove = new List<Point>();
                    for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++) if (a[y, x])
                            {
                                bool p2 = a[y - 1, x], p3 = a[y - 1, x + 1], p4 = a[y, x + 1], p5 = a[y + 1, x + 1],
                                     p6 = a[y + 1, x], p7 = a[y + 1, x - 1], p8 = a[y, x - 1], p9 = a[y - 1, x - 1];
                                // Exactly the same 8-neighbour Zhang-Suen test, without
                                // allocating bool[8] and a LINQ closure for each pixel.
                                int n = (p2 ? 1 : 0) + (p3 ? 1 : 0) + (p4 ? 1 : 0) + (p5 ? 1 : 0)
                                      + (p6 ? 1 : 0) + (p7 ? 1 : 0) + (p8 ? 1 : 0) + (p9 ? 1 : 0);
                                int trans = (!p2 && p3 ? 1 : 0) + (!p3 && p4 ? 1 : 0)
                                          + (!p4 && p5 ? 1 : 0) + (!p5 && p6 ? 1 : 0)
                                          + (!p6 && p7 ? 1 : 0) + (!p7 && p8 ? 1 : 0)
                                          + (!p8 && p9 ? 1 : 0) + (!p9 && p2 ? 1 : 0);
                                if (n < 2 || n > 6 || trans != 1) continue;
                                if (pass == 0) { if (p2 && p4 && p6) continue; if (p4 && p6 && p8) continue; }
                                else { if (p2 && p4 && p8) continue; if (p2 && p6 && p8) continue; }
                                remove.Add(new Point(x, y));
                            }
                    if (remove.Count > 0) changed = true;
                    foreach (Point p in remove) a[p.Y, p.X] = false;
                }
            } while (changed);
            return a;
        }
        private static double[,] DistanceMap(bool[,] sk)
        {
            int h = sk.GetLength(0), w = sk.GetLength(1);
            byte[,] src = new byte[h, w];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) src[y, x] = sk[y, x] ? (byte)0 : (byte)255;
            using (Mat mat = WriteGray(src)) using (Mat dist = new Mat())
            {
                CvInvoke.DistanceTransform(mat, dist, null, DistType.L2, 3);
                using (var im = dist.ToImage<Gray, float>())
                {
                    double[,] outp = new double[h, w];
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) outp[y, x] = im.Data[y, x, 0];
                    return outp;
                }
            }
        }
        // Python robust_trim(): clip to 7, retain the smallest floor(0.82 * n).
        // Input is a reusable per-character buffer; no allocation in each candidate.
        private static double TrimInPlace(double[] values, int n)
        {
            if (n == 0) return 10.0;
            int keep = Math.Max(1, (int)(n * .82));
            for (int k = 0; k < n; k++)
                if (values[k] > 7.0) values[k] = 7.0;
            int left = 0, right = n - 1, target = keep - 1;
            while (left < right)
            {
                double pivot = values[left + (right - left) / 2];
                int lt = left, i = left, gt = right;
                while (i <= gt)
                {
                    if (values[i] < pivot)
                    {
                        double t = values[lt]; values[lt++] = values[i]; values[i++] = t;
                    }
                    else if (values[i] > pivot)
                    {
                        double t = values[gt]; values[gt--] = values[i]; values[i] = t;
                    }
                    else i++;
                }
                if (target < lt) right = lt - 1;
                else if (target > gt) left = gt + 1;
                else break;
            }
            double sum = 0.0;
            for (int k = 0; k < keep; k++) sum += values[k];
            return sum / keep;
        }
        private static int TopBarCut(byte[,] g)
        {
            int h = g.GetLength(0), w = g.GetLength(1); if (h < 18 || w < 18) return 0;
            double[] fraction = new double[h];
            for (int y = 0; y < h; y++) { int count = 0; for (int x = 0; x < w; x++) if (g[y, x] > 0) count++; fraction[y] = (double)count / w; }
            if (fraction.Take(Math.Min(3, h)).Max() < .72) return 0;
            int maxrow = Math.Max(2, Math.Min((int)(h * .35), h - 6)), last = -1;
            for (int y = 0; y < maxrow; y++) if (fraction[y] > .64) last = y;
            if (last < 0 || last + 1 >= maxrow - 1) return 0;
            return Math.Min(h - 8, last + 3);
        }
        // Configurable odd-sized rectangular closing for temporary alignment masks ONLY.
        // Original Golden/Test pixel data and defect detection output are never modified.
        private static byte[,] ClosingForAlignment(byte[,] src, int kernelSize)
        {
            if (kernelSize < 1 || kernelSize % 2 == 0)
                throw new ArgumentOutOfRangeException("kernelSize", "Closing 核心必須是正奇數，例如 3、5、7。");
            if (kernelSize == 1) return src;
            int radius = kernelSize / 2;
            int h = src.GetLength(0), w = src.GetLength(1);
            byte[,] dilated = new byte[h, w];
            byte[,] closed = new byte[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool found = false;
                    for (int dy = -radius; dy <= radius && !found; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int yy = y + dy, xx = x + dx;
                            if (yy >= 0 && yy < h && xx >= 0 && xx < w && src[yy, xx] != 0)
                            { found = true; break; }
                        }
                    dilated[y, x] = found ? (byte)255 : (byte)0;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool all = true;
                    for (int dy = -radius; dy <= radius && all; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int yy = y + dy, xx = x + dx;
                            if (yy < 0 || yy >= h || xx < 0 || xx >= w || dilated[yy, xx] == 0)
                            { all = false; break; }
                        }
                    closed[y, x] = all ? (byte)255 : (byte)0;
                }
            return closed;
        }
        public static Match BandMatch(byte[,] golden, byte[,] test, Rectangle roi,
            int xRadius = 48, int yRadius = 12, int searchPad = 12, int maxXRadius = 96, bool retry = false,
            bool applyClosingForAlignment = false, int closingKernelSize = 5)
        {
            int x0 = roi.X, y0 = roi.Y, W = roi.Width, H = roi.Height;
            int gh = golden.GetLength(0), gw = golden.GetLength(1), th = test.GetLength(0), tw = test.GetLength(1);
            if (x0 < 0 || y0 < 0 || x0 + W > gw || y0 + H > gh || W <= 0 || H <= 0) return null;
            byte[,] g = Slice(golden, x0, y0, W, H);
            int bx0 = Math.Max(0, x0 - xRadius - searchPad), bx1 = Math.Min(tw, x0 + W + xRadius + searchPad);
            int by0 = Math.Max(0, y0 - yRadius - searchPad), by1 = Math.Min(th, y0 + H + yRadius + searchPad);
            if (bx1 - bx0 < W || by1 - by0 < H) return null;
            byte[,] source = Slice(test, bx0, by0, bx1 - bx0, by1 - by0);
            // Top bar and ALL output/debug patches are taken from the original data.
            // Only the masks fed to Skeleton / Chamfer use optional closing.
            int bar = TopBarCut(g);
            bool[,] gs = Skeleton(applyClosingForAlignment ? ClosingForAlignment(g, closingKernelSize) : g);
            bool[,] ts = Skeleton(applyClosingForAlignment ? ClosingForAlignment(source, closingKernelSize) : source);
            for (int y = 0; y < bar; y++) for (int x = 0; x < W; x++) gs[y, x] = false;
            var gy = new List<Point>(); for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (gs[y, x]) gy.Add(new Point(x, y));
            if (gy.Count < 8) return null;
            double[,] dg = DistanceMap(gs), dt = DistanceMap(ts);
            // Sparse test-skeleton index: rows retain their original ascending X order.
            // Thus each candidate visits exactly the same active pixels as the old
            // for(y) for(x) scan; no sampling, early pruning, or score changes.
            int sourceHeight = ts.GetLength(0), sourceWidth = ts.GetLength(1);
            int[][] testSkeletonRows = new int[sourceHeight][];
            for (int yy = 0; yy < sourceHeight; yy++)
            {
                int count = 0;
                for (int xx = 0; xx < sourceWidth; xx++) if (ts[yy, xx]) count++;
                var skeletonRowXs = new int[count];
                int k = 0;
                for (int xx = 0; xx < sourceWidth; xx++) if (ts[yy, xx]) skeletonRowXs[k++] = xx;
                testSkeletonRows[yy] = skeletonRowXs;
            }
            // The buffers belong to this BandMatch call. Parallel.For invokes a
            // separate BandMatch for each character, so no shared mutable memory.
            double[] forwardBuffer = new double[gy.Count];
            double[] backwardBuffer = new double[W * H];
            var cache = new Dictionary<long, double>();
            Func<int, int, double> ev = (dx, dy) => {
                long key = ((long)dx << 32) | (uint)dy;
                double value;
                if (cache.TryGetValue(key, out value)) return value;
                int sx = x0 + dx - bx0, sy = y0 + dy - by0;
                if (sx < 0 || sy < 0 || sx + W > bx1 - bx0 || sy + H > by1 - by0)
                    return cache[key] = 99.0;
                int forwardCount = 0;
                foreach (Point point in gy)
                    forwardBuffer[forwardCount++] = dt[sy + point.Y, sx + point.X];
                int testCount = 0;
                for (int y = bar; y < H; y++)
                {
                    int[] rowXs = testSkeletonRows[sy + y];
                    // Binary search for first skeleton X >= sx.
                    int lo = 0, hi = rowXs.Length;
                    while (lo < hi)
                    {
                        int mid = lo + ((hi - lo) >> 1);
                        if (rowXs[mid] < sx) lo = mid + 1;
                        else hi = mid;
                    }
                    int stop = sx + W;
                    for (int k = lo; k < rowXs.Length && rowXs[k] < stop; k++)
                        backwardBuffer[testCount++] = dg[y, rowXs[k] - sx];
                }
                if (testCount < 8) return cache[key] = 99.0;
                double ratio = (double)testCount / gy.Count;
                double penalty = 1.6 * Math.Max(0, .58 - ratio)
                               + .7 * Math.Max(0, ratio - 2.0);
                return cache[key] = .5 * (TrimInPlace(forwardBuffer, forwardCount)
                                          + TrimInPlace(backwardBuffer, testCount)) + penalty;
            };
            var options = new List<Candidate>();
            Action<int, int> evaluate = (dx, dy) => { double score = ev(dx, dy); options.Add(new Candidate { Dx = dx, Dy = dy, Score = score, Penalized = score + .001 * (Math.Abs(dx) + Math.Abs(dy)) }); };
            for (int dy = -yRadius; dy <= yRadius; dy += 3) for (int dx = -xRadius; dx <= xRadius; dx += 3) evaluate(dx, dy);
            // Python includes zero separately, including when radius isn't divisible by 3.
            var xs = Enumerable.Range(0, xRadius * 2 / 3 + 2).Select(i => -xRadius + 3 * i).Where(i => i <= xRadius).ToList();
            var ys = Enumerable.Range(0, yRadius * 2 / 3 + 2).Select(i => -yRadius + 3 * i).Where(i => i <= yRadius).ToList();
            if (!xs.Contains(0)) foreach (int dy in ys) evaluate(0, dy);
            if (!ys.Contains(0)) foreach (int dx in xs) evaluate(dx, 0);
            if (!xs.Contains(0) && !ys.Contains(0)) evaluate(0, 0);
            Func<IEnumerable<Candidate>, List<Candidate>> sorted = a => a.OrderBy(q => q.Penalized).ThenBy(q => q.Score).ThenBy(q => q.Dx).ThenBy(q => q.Dy).ToList();
            var centers = new List<Candidate>();
            foreach (Candidate c in sorted(options))
            {
                if (centers.All(other => Math.Abs(c.Dx - other.Dx) > 5 || Math.Abs(c.Dy - other.Dy) > 4)) centers.Add(c);
                if (centers.Count >= 4) break;
            }
            foreach (Candidate c in centers)
                for (int dy = Math.Max(-yRadius, c.Dy - 3); dy <= Math.Min(yRadius, c.Dy + 3); dy++)
                    for (int dx = Math.Max(-xRadius, c.Dx - 3); dx <= Math.Min(xRadius, c.Dx + 3); dx++) evaluate(dx, dy);
            Candidate best = sorted(options)[0]; double before = ev(0, 0);
            bool xBoundary = Math.Abs(best.Dx) >= xRadius - 1;
            if (xBoundary && !retry && maxXRadius > xRadius)
                return BandMatch(golden, test, roi, Math.Min(maxXRadius, Math.Max(xRadius + 20, xRadius * 2)), yRadius, searchPad, maxXRadius, true, applyClosingForAlignment, closingKernelSize);
            bool boundary = xBoundary || Math.Abs(best.Dy) >= yRadius - 1;
            bool accepted = best.Score < 2.7 && !boundary && (before - best.Score >= .15 || (best.Dx == 0 && best.Dy == 0));
            int udx = accepted ? best.Dx : 0, udy = accepted ? best.Dy : 0;
            return new Match
            {
                BestDx = best.Dx,
                BestDy = best.Dy,
                UsedDx = udx,
                UsedDy = udy,
                Score = best.Score,
                ClosingUsedForAlignment = applyClosingForAlignment,
                ClosingKernelSize = applyClosingForAlignment ? closingKernelSize : 0,
                Before = before,
                Accepted = accepted,
                SearchRadiusUsed = xRadius,
                Reason = boundary ? "boundary" : !accepted ? "weak_or_poor_match" : "accepted",
                GoldenRoi = roi,
                Golden = g,
                Original = Slice(test, x0, y0, W, H),
                Best = Slice(test, x0 + best.Dx, y0 + best.Dy, W, H),
                Used = Slice(test, x0 + udx, y0 + udy, W, H)
            };
        }
    }
}

// ===== NativeCharacterAlignmentPipeline.cs =====
// C#-only translation of Python top-level alignment sequence; does not execute Python.
// API accepts already-cropped Yin/Yang areas from the existing C# camera pipeline.
namespace StandardOPage
{
    public static class NativeCharacterAlignmentPipeline
    {
        public sealed class CharacterResult
        {
            public int FontPt, CharacterIndex;
            public CharacterBandMatcher.Match Alignment;
        }
        public sealed class GroupResult
        {
            public string Side, Group;
            public int StripStartX;
            public double EccRotation;
            public byte[,] Golden, Aligned, AlignedGray;
            public List<CharacterResult> Characters = new List<CharacterResult>();
        }
        // Diagnostic output only. Does not change matching, scoring, acceptance or parallel work.
        // DEBUG ONLY: visualize the CSV Golden ROI and the search envelope for each glyph.
        // All rectangles are drawn on a COPY of the already computed overlay.
        private static void SaveGlyphRoiBoxes(GroupResult group, string mode)
        {
            if (!FontRoiBoxDebugSettings.Enabled) return;
            try
            {
                string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "FontAlignment_ROI_Debug", mode, group.Side, group.Group);
                Directory.CreateDirectory(root);
                int w = group.Golden.GetLength(1), h = group.Golden.GetLength(0);
                var frame = new Rectangle(0, 0, w, h);
                var csv = new StringBuilder("side,group,font_pt,char_index,roi_x,roi_y,roi_width,roi_height,search_x,search_y,search_width,search_height,best_dx,best_dy,used_dx,used_dy,accepted");
                csv.AppendLine();
                using (Mat overlay = CharacterBandMatcher.BuildFinalOverlay(group.Golden, group.Aligned))
                {
                    foreach (var ptGroup in group.Characters.GroupBy(c => c.FontPt).OrderBy(g => g.Key))
                    {
                        using (Mat ptCanvas = overlay.Clone())
                        {
                            foreach (CharacterResult row in ptGroup)
                            {
                                var m = row.Alignment;
                                if (m == null) continue;
                                Rectangle roi = Rectangle.Intersect(frame, m.GoldenRoi);
                                int radiusX = m.SearchRadiusUsed;
                                // The current normal per-character vertical search radius is 12 px.
                                int radiusY = FontRoiBoxDebugSettings.SearchYRadius;
                                Rectangle search = Rectangle.Intersect(frame,
                                    new Rectangle(m.GoldenRoi.X - radiusX, m.GoldenRoi.Y - radiusY,
                                        m.GoldenRoi.Width + 2 * radiusX,
                                        m.GoldenRoi.Height + 2 * radiusY));
                                // BGR blue: candidate search envelope. Yellow: Golden CSV ROI.
                                if (search.Width > 0 && search.Height > 0)
                                    CvInvoke.Rectangle(ptCanvas, search, new MCvScalar(255, 150, 0), 1);
                                if (roi.Width > 0 && roi.Height > 0)
                                    CvInvoke.Rectangle(ptCanvas, roi, new MCvScalar(0, 255, 255), 1);
                                string label = row.FontPt.ToString(CultureInfo.InvariantCulture) + "pt #" +
                                    row.CharacterIndex.ToString(CultureInfo.InvariantCulture);
                                if (roi.Width > 0 && roi.Height > 0)
                                    CvInvoke.PutText(ptCanvas, label,
                                        new Point(roi.X, Math.Max(11, roi.Y - 3)),
                                        FontFace.HersheySimplex, 0.36, new MCvScalar(255, 255, 255), 1);
                                csv.AppendLine(string.Join(",", new[] {
                                group.Side, group.Group,
                                row.FontPt.ToString(CultureInfo.InvariantCulture),
                                row.CharacterIndex.ToString(CultureInfo.InvariantCulture),
                                m.GoldenRoi.X.ToString(CultureInfo.InvariantCulture),
                                m.GoldenRoi.Y.ToString(CultureInfo.InvariantCulture),
                                m.GoldenRoi.Width.ToString(CultureInfo.InvariantCulture),
                                m.GoldenRoi.Height.ToString(CultureInfo.InvariantCulture),
                                search.X.ToString(CultureInfo.InvariantCulture),
                                search.Y.ToString(CultureInfo.InvariantCulture),
                                search.Width.ToString(CultureInfo.InvariantCulture),
                                search.Height.ToString(CultureInfo.InvariantCulture),
                                m.BestDx.ToString(CultureInfo.InvariantCulture),
                                m.BestDy.ToString(CultureInfo.InvariantCulture),
                                m.UsedDx.ToString(CultureInfo.InvariantCulture),
                                m.UsedDy.ToString(CultureInfo.InvariantCulture),
                                m.Accepted ? "1" : "0" }));
                            }
                            ptCanvas.Save(Path.Combine(root,
                                ptGroup.Key.ToString("00", CultureInfo.InvariantCulture) + "pt_ROI_BOXES.png"));
                        }
                    }
                }
                File.WriteAllText(Path.Combine(root, "ROI_BOXES.csv"), csv.ToString(), new UTF8Encoding(true));
                Debug.WriteLine("[NativeFont][ROI] saved " + root);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[NativeFont][ROI] debug-only export failed: " + ex.Message);
            }
        }

        private static void SaveLargeADiagnostics(GroupResult group, string mode)
        {
            // Bottom-most index is the first alphabetical A in each vertical font column.
            // Verify against Golden CSV if the lettering order changes.
            foreach (int pt in new[] { 11, 12 })
            {
                var rows = group.Characters.Where(c => c.FontPt == pt).ToList();
                if (rows.Count == 0) continue;
                var row = rows.OrderByDescending(c => c.CharacterIndex).First();
                var m = row.Alignment;
                if (m == null) continue;
                try
                {
                    string closingTag = m.ClosingUsedForAlignment ? ("CLOSING" + m.ClosingKernelSize.ToString(CultureInfo.InvariantCulture)) : "BASELINE";
                    string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FontAlignment_ADebug", mode,
                        closingTag, group.Side, group.Group);
                    Directory.CreateDirectory(root);
                    string stem = pt.ToString("00") + "pt_" + row.CharacterIndex.ToString("000") + "_A";
                    // Each tile uses the exact same Golden ROI dimensions. Test candidates
                    // are already retained by BandMatch, so no new search is introduced.
                    int h = m.Golden.GetLength(0), w = m.Golden.GetLength(1);
                    int factor = 3, title = 28, margin = 8;
                    int tw = w * factor, th = h * factor + title;
                    using (var sheet = new Image<Bgr, byte>(tw * 3 + margin * 4, th * 2 + margin * 3))
                    {
                        sheet.SetZero();
                        var panels = new List<byte[,]> { m.Golden, m.Original, m.Best, m.Used };
                        string[] captions = { "GOLDEN", "TEST BEFORE", "BEST CANDIDATE", "USED CANDIDATE", "PIXEL OVERLAY", "SKELETON OVERLAY" };
                        byte[,,] pixel = null, skel = null;
                        using (Mat po = CharacterBandMatcher.BuildFinalOverlay(m.Golden, m.Used))
                        using (Mat so = CharacterBandMatcher.BuildSkeletonOverlay(m.Golden, m.Used))
                        using (var pi = po.ToImage<Bgr, byte>())
                        using (var si = so.ToImage<Bgr, byte>())
                        {
                            pixel = pi.Data; skel = si.Data;
                            for (int panel = 0; panel < 6; panel++)
                            {
                                int ox = margin + (panel % 3) * (tw + margin);
                                int oy = margin + (panel / 3) * (th + margin);
                                CvInvoke.PutText(sheet, captions[panel], new Point(ox + 2, oy + 19),
                                    FontFace.HersheySimplex, 0.45, new MCvScalar(230, 230, 230), 1);
                                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                                    {
                                        byte b = 0, g = 0, r = 0;
                                        if (panel < 4) { b = g = r = panels[panel][y, x]; }
                                        else
                                        {
                                            var data = panel == 4 ? pixel : skel;
                                            b = data[y, x, 0]; g = data[y, x, 1]; r = data[y, x, 2];
                                        }
                                        for (int yy = 0; yy < factor; yy++) for (int xx = 0; xx < factor; xx++)
                                            {
                                                int py = oy + title + y * factor + yy, px = ox + x * factor + xx;
                                                sheet.Data[py, px, 0] = b;
                                                sheet.Data[py, px, 1] = g;
                                                sheet.Data[py, px, 2] = r;
                                            }
                                    }
                            }
                        }
                        sheet.Save(Path.Combine(root, stem + "_comparison.png"));
                    }
                    string rowCsv = "mode,closing,side,group,font_pt,char_index,x,y,width,height,best_dx,best_dy,used_dx,used_dy,accepted,before,score,gain,search_radius,reason" + Environment.NewLine
                        + string.Join(",", new string[] {
                            mode, closingTag, group.Side, group.Group, pt.ToString(), row.CharacterIndex.ToString(),
                            m.GoldenRoi.X.ToString(),m.GoldenRoi.Y.ToString(), m.GoldenRoi.Width.ToString(), m.GoldenRoi.Height.ToString(),
                            m.BestDx.ToString(),m.BestDy.ToString(),m.UsedDx.ToString(),m.UsedDy.ToString(),
                            m.Accepted ? "1" : "0", m.Before.ToString("F6", CultureInfo.InvariantCulture),
                            m.Score.ToString("F6", CultureInfo.InvariantCulture),
                            (m.Before - m.Score).ToString("F6", CultureInfo.InvariantCulture),
                            m.SearchRadiusUsed.ToString(), m.Reason }) + Environment.NewLine;
                    File.WriteAllText(Path.Combine(root, stem + "_metrics.csv"), rowCsv, new UTF8Encoding(true));
                    Debug.WriteLine("[NativeFont][A-Debug] saved " + mode + "/" + closingTag + " " + group.Side + "/" + group.Group + " " + stem
                        + " best=(" + m.BestDx + "," + m.BestDy + ") used=(" + m.UsedDx + "," + m.UsedDy + ") accepted=" + m.Accepted);
                }
                catch (Exception ex)
                {
                    // Diagnostics must NEVER terminate inspection.
                    Debug.WriteLine("[NativeFont][A-Debug] file output failed: " + ex.Message);
                }
            }
        }

        private static double Corr(double[] a, double[] b)
        {
            int n = a.Length; if (b.Length != n) return -1;
            double ma = a.Average(), mb = b.Average(), aa = 0, bb = 0, ab = 0;
            for (int i = 0; i < n; i++) { double x = a[i] - ma, y = b[i] - mb; aa += x * x; bb += y * y; ab += x * y; }
            return aa > 1e-9 && bb > 1e-9 ? ab / Math.Sqrt(aa * bb) : -1;
        }
        private static (byte[,] strip, int startX) Coarse(byte[,] area, byte[,] reference)
        {
            int ah = area.GetLength(0), aw = area.GetLength(1), rh = reference.GetLength(0), rw = reference.GetLength(1);
            if (ah < rh || aw < rw) throw new ArgumentException("Measured image smaller than reference");
            using (Mat a = CharacterBandMatcher.WriteGray(area)) using (Mat b = CharacterBandMatcher.WriteGray(reference))
            using (Mat a2 = new Mat()) using (Mat b2 = new Mat()) using (Mat match = new Mat())
            {
                int sw = (int)Math.Round(aw * .5, MidpointRounding.ToEven), sh = (int)Math.Round(ah * .5, MidpointRounding.ToEven);
                int tw = (int)Math.Round(rw * .5, MidpointRounding.ToEven), th = (int)Math.Round(rh * .5, MidpointRounding.ToEven);
                CvInvoke.Resize(a, a2, new Size(sw, sh), 0, 0, Inter.Nearest);
                CvInvoke.Resize(b, b2, new Size(tw, th), 0, 0, Inter.Nearest);
                CvInvoke.MatchTemplate(a2, b2, match, TemplateMatchingType.CcorrNormed);
                double min = 0, max = 0; Point minLoc = new Point(), maxLoc = new Point();
                CvInvoke.MinMaxLoc(match, ref min, ref max, ref minLoc, ref maxLoc);
                int x = Math.Max(0, Math.Min(aw - rw, maxLoc.X * 2));
                // Original Python projection uses 1 row in every 4 rows, and full-resolution X.
                double[] A = new double[aw], B = new double[rw];
                for (int y = 0; y < ah; y += 4) for (int k = 0; k < aw; k++) if (area[y, k] > 0) A[k]++;
                for (int y = 0; y < rh; y += 4) for (int k = 0; k < rw; k++) if (reference[y, k] > 0) B[k]++;
                double best = -1; int bestX = -1;
                for (int start = Math.Max(0, x - 20); start <= Math.Min(aw - rw, x + 20); start++)
                {
                    double[] section = new double[rw]; Array.Copy(A, start, section, 0, rw);
                    double corr = Corr(section, B);
                    if (corr > best) { best = corr; bestX = start; }
                }
                if (bestX >= 0 && best > .5) x = bestX;
                return (CharacterBandMatcher.Slice(area, x, 0, rw, ah), x);
            }
        }
        private static double[,] RotateSeed(int w, int h, double angle, double dx, double dy)
        {
            double theta = angle * Math.PI / 180.0, c = Math.Cos(theta), s = Math.Sin(theta);
            // OpenCV getRotationMatrix2D(center=(w/2,h/2), angle, scale=1).
            return new double[,] { { c, s, (1 - c) * w / 2.0 - s * h / 2.0 + dx }, { -s, c, s * w / 2.0 + (1 - c) * h / 2.0 + dy } };
        }
        private static Matrix<float> FloatMatrix(double[,] m)
        {
            var mat = new Matrix<float>(2, 3);
            for (int i = 0; i < 2; i++) for (int j = 0; j < 3; j++) mat[i, j] = (float)m[i, j];
            return mat;
        }
        private static double[,] Invert(double[,] m)
        {
            double det = m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0];
            double a = m[1, 1] / det, b = -m[0, 1] / det, c = -m[1, 0] / det, d = m[0, 0] / det;
            return new double[,] { { a, b, -a * m[0, 2] - b * m[1, 2] }, { c, d, -c * m[0, 2] - d * m[1, 2] } };
        }
        private static byte[,] Warp(byte[,] src, double[,] map, int w, int h, Inter interpolation)
        {
            using (Mat a = CharacterBandMatcher.WriteGray(src)) using (Mat result = new Mat()) using (var M = FloatMatrix(map))
            {
                CvInvoke.WarpAffine(a, result, M, new Size(w, h), interpolation, Emgu.CV.CvEnum.Warp.Default, BorderType.Constant, new MCvScalar(0));
                return CharacterBandMatcher.ReadGray(result);
            }
        }
        private static double Chamfer(byte[,] a, byte[,] b)
        {
            // Edge-image bidirectional Chamfer on the small ECC assessment image.
            using (Mat aa = CharacterBandMatcher.WriteGray(a)) using (Mat bb = CharacterBandMatcher.WriteGray(b))
            using (Mat ga = new Mat()) using (Mat gb = new Mat()) using (Mat ea = new Mat()) using (Mat eb = new Mat())
            {
                CvInvoke.GaussianBlur(aa, ga, new Size(3, 3), 0);
                CvInvoke.GaussianBlur(bb, gb, new Size(3, 3), 0);
                CvInvoke.Canny(ga, ea, 60, 150); CvInvoke.Canny(gb, eb, 60, 150);
                byte[,] x = CharacterBandMatcher.ReadGray(ea), y = CharacterBandMatcher.ReadGray(eb);
                int h = x.GetLength(0), w = x.GetLength(1);
                if (h > 40 && w > 20) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) if (j < 18 || j >= h - 18 || i < 8 || i >= w - 8) { x[j, i] = 0; y[j, i] = 0; } }
                int nx = 0, ny = 0;
                byte[,] px = new byte[h, w], py = new byte[h, w];
                for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                    {
                        if (x[j, i] != 0) { nx++; px[j, i] = 0; } else px[j, i] = 255;
                        if (y[j, i] != 0) { ny++; py[j, i] = 0; } else py[j, i] = 255;
                    }
                if (nx < 30 || ny < 30) return 999;
                using (Mat ax = CharacterBandMatcher.WriteGray(px)) using (Mat ay = CharacterBandMatcher.WriteGray(py))
                using (Mat dx = new Mat()) using (Mat dy = new Mat())
                {
                    CvInvoke.DistanceTransform(ax, dx, null, DistType.L2, 3);
                    CvInvoke.DistanceTransform(ay, dy, null, DistType.L2, 3);
                    using (var dxa = dx.ToImage<Gray, float>()) using (var dya = dy.ToImage<Gray, float>())
                    {
                        double sx = 0, sy = 0;
                        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                            {
                                if (x[j, i] > 0) sx += Math.Min(dya.Data[j, i, 0], 8);
                                if (y[j, i] > 0) sy += Math.Min(dxa.Data[j, i, 0], 8);
                            }
                        return .5 * (sx / nx + sy / ny);
                    }
                }
            }
        }

        // Test-side Chamfer data does not change among the three ECC seeds.
        // Keep precisely the existing 3x3 blur, Canny(60,150), border exclusion,
        // DistTransform(L2,3), and float accumulation order.
        private sealed class CachedChamferTest
        {
            public byte[,] Edges;
            public float[,,] Distance;
            public int Count;
        }
        private static CachedChamferTest BuildChamferTest(byte[,] b)
        {
            using (Mat bb = CharacterBandMatcher.WriteGray(b))
            using (Mat gb = new Mat()) using (Mat eb = new Mat())
            {
                CvInvoke.GaussianBlur(bb, gb, new Size(3, 3), 0);
                CvInvoke.Canny(gb, eb, 60, 150);
                byte[,] edges = CharacterBandMatcher.ReadGray(eb);
                int h = edges.GetLength(0), w = edges.GetLength(1), count = 0;
                byte[,] inv = new byte[h, w];
                bool trim = h > 40 && w > 20;
                for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                    {
                        if (trim && (j < 18 || j >= h - 18 || i < 8 || i >= w - 8)) edges[j, i] = 0;
                        if (edges[j, i] != 0) { count++; inv[j, i] = 0; }
                        else inv[j, i] = 255;
                    }
                using (Mat src = CharacterBandMatcher.WriteGray(inv)) using (Mat dst = new Mat())
                {
                    CvInvoke.DistanceTransform(src, dst, null, DistType.L2, 3);
                    using (var img = dst.ToImage<Gray, float>())
                        return new CachedChamferTest
                        {
                            Edges = edges,
                            Count = count,
                            Distance = (float[,,])img.Data.Clone()
                        };
                }
            }
        }
        private static double ChamferCached(byte[,] a, CachedChamferTest testCache)
        {
            using (Mat aa = CharacterBandMatcher.WriteGray(a))
            using (Mat ga = new Mat()) using (Mat ea = new Mat())
            {
                CvInvoke.GaussianBlur(aa, ga, new Size(3, 3), 0);
                CvInvoke.Canny(ga, ea, 60, 150);
                byte[,] x = CharacterBandMatcher.ReadGray(ea);
                int h = x.GetLength(0), w = x.GetLength(1), nx = 0;
                byte[,] px = new byte[h, w];
                bool trim = h > 40 && w > 20;
                for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                    {
                        if (trim && (j < 18 || j >= h - 18 || i < 8 || i >= w - 8)) x[j, i] = 0;
                        if (x[j, i] != 0) { nx++; px[j, i] = 0; }
                        else px[j, i] = 255;
                    }
                if (nx < 30 || testCache.Count < 30) return 999;
                using (Mat ax = CharacterBandMatcher.WriteGray(px)) using (Mat dx = new Mat())
                {
                    CvInvoke.DistanceTransform(ax, dx, null, DistType.L2, 3);
                    using (var dxa = dx.ToImage<Gray, float>())
                    {
                        double sx = 0, sy = 0;
                        // Exactly the original pixel traversal and float-to-double summation.
                        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                            {
                                if (x[j, i] > 0) sx += Math.Min(testCache.Distance[j, i, 0], 8);
                                if (testCache.Edges[j, i] > 0) sy += Math.Min(dxa.Data[j, i, 0], 8);
                            }
                        return .5 * (sx / nx + sy / testCache.Count);
                    }
                }
            }
        }
        private static byte[,] WarpFromMat(Mat src, double[,] map, int w, int h, Inter interpolation)
        {
            using (Mat result = new Mat()) using (var M = FloatMatrix(map))
            {
                CvInvoke.WarpAffine(src, result, M, new Size(w, h), interpolation,
                    Emgu.CV.CvEnum.Warp.Default, BorderType.Constant, new MCvScalar(0));
                return CharacterBandMatcher.ReadGray(result);
            }
        }

        // Preserve the original grayscale Test under exactly the transform used for
        // the binary group alignment. This does not change matching or scoring.
        private static byte[,] WarpOriginalGray(byte[,] gray, double[,] inverse,
            int w, int h, bool isYin)
        {
            using (Mat src = CharacterBandMatcher.WriteGray(gray))
            using (Mat output = new Mat())
            using (var m = FloatMatrix(inverse))
            {
                CvInvoke.WarpAffine(src, output, m, new Size(w, h), Inter.Nearest,
                    Emgu.CV.CvEnum.Warp.Default, BorderType.Constant,
                    new MCvScalar(isYin ? 0 : 255));
                return CharacterBandMatcher.ReadGray(output);
            }
        }

        // Process-wide budget: Yin and Yang may run concurrently. Restrict the
        // aggregate CPU-heavy glyph matches instead of allowing 4 + 4 workers.
        // This semaphore is shared across both sides, even when Analyze is
        // called from different application threads.
        private static readonly SemaphoreSlim GlyphCpuBudget = new SemaphoreSlim(4, 4);

        private static (byte[,] aligned, byte[,] gray, double angle) EccRigid(byte[,] golden, byte[,] test, byte[,] originalGray, string side, string group, double scale = .33)
        {
            var totalTimer = Stopwatch.StartNew();
            long prepMs = 0, phaseMs = 0, scoringMs = 0, warpMs = 0, eccMs = 0;
            int converged = 0, failed = 0;
            int h = golden.GetLength(0), w = golden.GetLength(1);
            int sw = Math.Max(32, (int)Math.Round(w * scale, MidpointRounding.ToEven));
            int sh = Math.Max(32, (int)Math.Round(h * scale, MidpointRounding.ToEven));
            using (Mat gg = CharacterBandMatcher.WriteGray(golden)) using (Mat tt = CharacterBandMatcher.WriteGray(test))
            using (Mat g0 = new Mat()) using (Mat t0 = new Mat()) using (Mat ga = new Mat()) using (Mat ta = new Mat())
            using (Mat gf = new Mat()) using (Mat tf = new Mat())
            {
                CvInvoke.Resize(gg, g0, new Size(sw, sh)); CvInvoke.Resize(tt, t0, new Size(sw, sh));
                CvInvoke.GaussianBlur(g0, ga, new Size(3, 3), 0); CvInvoke.GaussianBlur(t0, ta, new Size(3, 3), 0);
                ga.ConvertTo(gf, DepthType.Cv32F, 1.0 / 255.0); ta.ConvertTo(tf, DepthType.Cv32F, 1.0 / 255.0);
                prepMs = totalTimer.ElapsedMilliseconds;
                var phaseTimer = Stopwatch.StartNew();
                double response; var phase = CvInvoke.PhaseCorrelate(gf, tf, null, out response);
                phaseMs = phaseTimer.ElapsedMilliseconds;
                // Cache both the unchanged assessment images and the test-side
                // Canny/DistanceTransform across the three ECC seeds.
                // Previous implementation copied both Mat images into managed arrays
                // on EVERY successful angle candidate (six large copies per group).
                byte[,] grayReferenceSmall = CharacterBandMatcher.ReadGray(ga);
                byte[,] grayTestSmall = CharacterBandMatcher.ReadGray(ta);
                var cachedTest = BuildChamferTest(grayTestSmall);
                double best = double.MaxValue; double[,] bestForward = null;
                foreach (double angle in new double[] { -1, 0, 1 })
                {
                    try
                    {
                        using (var init = FloatMatrix(RotateSeed(sw, sh, angle, -phase.X, -phase.Y)))
                        {
                            // Python: ECC(template=test, input=golden, seed=test->golden)
                            // OpenCV returns map for WARP_INVERSE_MAP; invert to compare as in Python.
                            var eccTimer = Stopwatch.StartNew();
                            try { CvInvoke.FindTransformECC(tf, gf, init, MotionType.Euclidean, new MCvTermCriteria(65, 1e-5)); }
                            finally { eccMs += eccTimer.ElapsedMilliseconds; }
                            converged++;
                            var scoreTimer = Stopwatch.StartNew();
                            double[,] inv = new double[2, 3]; for (int i = 0; i < 2; i++) for (int j = 0; j < 3; j++) inv[i, j] = init[i, j];
                            double[,] forward = Invert(inv);
                            byte[,] wimg = Warp(grayReferenceSmall, forward, sw, sh, Inter.Linear);
                            double score = ChamferCached(wimg, cachedTest);
                            scoringMs += scoreTimer.ElapsedMilliseconds;
                            if (score < best) { best = score; bestForward = forward; }
                        }
                    }
                    catch (Exception) { failed++; continue; }
                }
                if (bestForward == null) throw new InvalidOperationException("ECC did not converge");
                // S_inv * M * S (allows slight actual x/y scaling differences).
                double sx = (double)sw / w, sy = (double)sh / h;
                double[,] M ={{bestForward[0,0],bestForward[0,1]*sy/sx,bestForward[0,2]/sx},
                             {bestForward[1,0]*sx/sy,bestForward[1,1],bestForward[1,2]/sy}};
                double rotation = Math.Atan2(M[0, 1], M[0, 0]) * 180 / Math.PI;
                // Python: inverse map is used for final test-to-Golden warp.
                var finalTimer = Stopwatch.StartNew();
                byte[,] aligned = WarpFromMat(tt, Invert(M), w, h, Inter.Nearest);
                warpMs = finalTimer.ElapsedMilliseconds;
                Debug.WriteLine("[NativeFont][" + side + "][" + group + "] ECC breakdown ms: prep=" + prepMs
                    + ", phase=" + phaseMs + ", eccNative=" + eccMs + ", chamferWarp=" + scoringMs
                    + ", finalWarp=" + warpMs + ", other=" + Math.Max(0, totalTimer.ElapsedMilliseconds - prepMs - phaseMs - eccMs - scoringMs - warpMs)
                    + ", total=" + totalTimer.ElapsedMilliseconds + ", converged=" + converged + ", failed=" + failed
                    + ", small=" + sw + "x" + sh);
                return (aligned, WarpOriginalGray(originalGray, Invert(M), w, h, side == "Yin"), rotation);
            }
        }
        // Default to NCC-only group translation for production evaluation.
        // Switch back to original ECC without rebuilding: set environment variable
        // UNIVACCO_FONT_GROUP_ALIGNMENT=ECC and restart the application.
        private static string GroupAlignmentMode()
        {
            string mode = (Environment.GetEnvironmentVariable("UNIVACCO_FONT_GROUP_ALIGNMENT") ?? "NCC").Trim().ToUpperInvariant();
            return mode == "ECC" ? "ECC" : "NCC";
        }

        // Mirrors the Python 3-mode experiment's register_ncc_translation():
        // reduced grayscale/binary image, middle ROI, padded search with
        // TM_CCORR_NORMED, translation only (no rotation or scaling).
        // Transform returned by that Python function is Golden->Test; apply
        // its inverse (Test->Golden) when warping the actual Test image.
        private static (byte[,] aligned, byte[,] gray, double angle) NccTranslation(
            byte[,] golden, byte[,] test, byte[,] originalGray, string side, string group,
            double scale = .33, int radius = 60)
        {
            var timer = Stopwatch.StartNew();
            int h = golden.GetLength(0), w = golden.GetLength(1);
            int sw = Math.Max(32, (int)Math.Round(w * scale, MidpointRounding.ToEven));
            int sh = Math.Max(32, (int)Math.Round(h * scale, MidpointRounding.ToEven));
            if (sw <= 8 || sh <= 8) throw new InvalidOperationException("NCC registration image too small");
            using (Mat goldMat = CharacterBandMatcher.WriteGray(golden))
            using (Mat testMat = CharacterBandMatcher.WriteGray(test))
            using (Mat goldSmall = new Mat()) using (Mat testSmall = new Mat())
            using (Mat padded = new Mat()) using (Mat scores = new Mat())
            {
                CvInvoke.Resize(goldMat, goldSmall, new Size(sw, sh), 0, 0, Inter.Linear);
                CvInvoke.Resize(testMat, testSmall, new Size(sw, sh), 0, 0, Inter.Linear);
                int mx = Math.Min(Math.Max(3, (int)Math.Round(sw * .03, MidpointRounding.ToEven)), Math.Max(3, sw / 6));
                int my = Math.Min(Math.Max(3, (int)Math.Round(sh * .03, MidpointRounding.ToEven)), Math.Max(3, sh / 6));
                int tw = sw - 2 * mx, th = sh - 2 * my;
                if (tw <= 0 || th <= 0) throw new InvalidOperationException("NCC inner template too small");
                int rx = Math.Max(1, (int)Math.Round((double)radius * sw / w, MidpointRounding.ToEven));
                int ry = Math.Max(1, (int)Math.Round((double)radius * sh / h, MidpointRounding.ToEven));
                using (Mat central = new Mat(goldSmall, new Rectangle(mx, my, tw, th)))
                {
                    CvInvoke.CopyMakeBorder(testSmall, padded, ry, ry, rx, rx, BorderType.Constant, new MCvScalar(0));
                    CvInvoke.MatchTemplate(padded, central, scores, TemplateMatchingType.CcorrNormed);
                    double min = 0, max = 0;
                    Point minLoc = new Point(), maxLoc = new Point();
                    CvInvoke.MinMaxLoc(scores, ref min, ref max, ref minLoc, ref maxLoc);
                    // Golden -> Test displacement (exact same pixel-center algebra as Python).
                    double dx = (maxLoc.X - rx - mx) * (double)w / sw;
                    double dy = (maxLoc.Y - ry - my) * (double)h / sh;
                    // Python main(): warpAffine(test, invertAffineTransform(M), ...)
                    double[,] inverse = { { 1.0, 0.0, -dx }, { 0.0, 1.0, -dy } };
                    byte[,] aligned = WarpFromMat(testMat, inverse, w, h, Inter.Nearest);
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] NCC translation ms="
                        + timer.ElapsedMilliseconds + ", dx=" + dx.ToString("F3", CultureInfo.InvariantCulture)
                        + ", dy=" + dy.ToString("F3", CultureInfo.InvariantCulture)
                        + ", score=" + max.ToString("F6", CultureInfo.InvariantCulture)
                        + ", small=" + sw + "x" + sh);
                    return (aligned, WarpOriginalGray(originalGray, inverse, w, h, side == "Yin"), 0.0);
                }
            }
        }

        public static List<GroupResult> Analyze(Mat alreadyCroppedArea, string goldenDirectory, string side,
            int xRadius = 48, int yRadius = 12, int maxXRadius = 96, double eccScale = .33)
        {
            if (side != "Yin" && side != "Yang") throw new ArgumentException("side must be Yin or Yang");
            Stopwatch watch = Stopwatch.StartNew();
            var loaded = GoldenCharacters.Load(goldenDirectory, side);
            Debug.WriteLine("[NativeFont][" + side + "] golden load ms=" + watch.ElapsedMilliseconds);
            byte[,] refbin = GoldenCharacters.Binary(loaded.reference, side == "Yin");
            using (Mat srcGray = new Mat())
            {
                if (alreadyCroppedArea.NumberOfChannels == 1) alreadyCroppedArea.CopyTo(srcGray);
                else CvInvoke.CvtColor(alreadyCroppedArea, srcGray, ColorConversion.Bgr2Gray);
                byte[,] fullGray = CharacterBandMatcher.ReadGray(srcGray);
                byte[,] imagebin = GoldenCharacters.Binary(fullGray, side == "Yin");
                var coarse = Coarse(imagebin, refbin);
                byte[,] coarseGray = CharacterBandMatcher.Slice(fullGray, coarse.startX, 0,
                    refbin.GetLength(1), fullGray.GetLength(0));
                Debug.WriteLine("[NativeFont][" + side + "] coarse elapsed ms=" + watch.ElapsedMilliseconds);
                var results = new List<GroupResult>();
                foreach (string group in new[] { "A", "B" })
                {
                    int first = group == "A" ? 3 : 9, last = group == "A" ? 8 : 12;
                    int left = loaded.xoffsets[first];
                    int right = last == 12 ? refbin.GetLength(1) : loaded.xoffsets[last + 1];
                    int len = right - left, height = refbin.GetLength(0);
                    byte[,] g = CharacterBandMatcher.Slice(refbin, left, 0, len, height);
                    byte[,] t = CharacterBandMatcher.Slice(coarse.strip, left, 0, len, coarse.strip.GetLength(0));
                    byte[,] tGray = CharacterBandMatcher.Slice(coarseGray, left, 0, len, coarseGray.GetLength(0));
                    Stopwatch eccWatch = Stopwatch.StartNew();
                    string alignMode = GroupAlignmentMode();
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] alignment mode=" + alignMode);
                    var ecc = alignMode == "ECC"
                        ? EccRigid(g, t, tGray, side, group, eccScale)
                        : NccTranslation(g, t, tGray, side, group, eccScale);
                    eccWatch.Stop();
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] " + alignMode + " stage ms=" + eccWatch.ElapsedMilliseconds
                        + ", cumulative ms=" + watch.ElapsedMilliseconds);
                    var result = new GroupResult
                    {
                        Side = side,
                        Group = group,
                        Golden = g,
                        Aligned = new byte[height, len],
                        AlignedGray = new byte[height, len],
                        EccRotation = ecc.angle,
                        StripStartX = coarse.startX
                    };
                    // Yang background must remain 255 BEFORE inverted binary thresholding.
                    if (side == "Yang")
                        for (int y = 0; y < height; y++)
                            for (int x = 0; x < len; x++) result.AlignedGray[y, x] = 255;
                    // Every glyph has an independent search, so compute matches concurrently.
                    // Never write to GroupResult/Aligned from worker threads. A deterministic
                    // single-threaded merge retains the original CSV/character ordering.
                    var glyphs = loaded.glyphs.Where(r => r.FontPt >= first && r.FontPt <= last)
                        .OrderBy(r => r.FontPt).ThenBy(r => r.Index).ToArray();
                    // 依上方 FontClosingSettings 設定指定字級與 Closing 核心。
                    // 原圖和瑕疵檢測完全不受影響。
                    int closingKernelSize = FontClosingSettings.KernelSize;
                    if (closingKernelSize < 1 || closingKernelSize % 2 == 0)
                        throw new InvalidOperationException("FontClosingSettings.KernelSize 必須是正奇數，例如 3、5、7。");
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] Closing="
                        + closingKernelSize + "x" + closingKernelSize
                        + ", enabled_pts=" + string.Join(",", FontClosingSettings.EnabledFontPts.OrderBy(p => p))
                        + ", active=" + glyphs.Count(glyph => FontClosingSettings.ShouldApply(glyph.FontPt))
                        + "/" + glyphs.Length + " (match-only)");
                    var matches = new CharacterBandMatcher.Match[glyphs.Length];
                    // Per-side <=3; process-wide <=4 including simultaneous Yin/Yang.
                    int workerCount = Math.Max(1, Math.Min(3, Environment.ProcessorCount));
                    Stopwatch glyphWatch = Stopwatch.StartNew();
                    Parallel.For(0, glyphs.Length,
                        new ParallelOptions { MaxDegreeOfParallelism = workerCount }, i =>
                        {
                            var glyph = glyphs[i];
                            int offset = loaded.xoffsets[glyph.FontPt] - left;
                            var roi = new Rectangle(offset + glyph.X0, glyph.Y0,
                                glyph.X1 - glyph.X0, glyph.Y1 - glyph.Y0);
                            GlyphCpuBudget.Wait();
                            try
                            {
                                matches[i] = CharacterBandMatcher.BandMatch(
                                    g, ecc.aligned, roi, xRadius, yRadius, 12, maxXRadius, false,
                                    FontClosingSettings.ShouldApply(glyph.FontPt), closingKernelSize);
                            }
                            finally { GlyphCpuBudget.Release(); }
                        });
                    glyphWatch.Stop();
                    for (int i = 0; i < glyphs.Length; i++)
                    {
                        var match = matches[i];
                        if (match == null) continue;
                        var glyph = glyphs[i];
                        var roi = match.GoldenRoi;
                        result.Characters.Add(new CharacterResult
                        {
                            FontPt = glyph.FontPt,
                            CharacterIndex = glyph.Index,
                            Alignment = match
                        });
                        for (int y = 0; y < roi.Height; y++)
                            for (int x = 0; x < roi.Width; x++)
                            {
                                result.Aligned[roi.Y + y, roi.X + x] = match.Used[y, x];
                                int sy = roi.Y + match.UsedDy + y;
                                int sx = roi.X + match.UsedDx + x;
                                if (sy >= 0 && sy < ecc.gray.GetLength(0) && sx >= 0 && sx < ecc.gray.GetLength(1))
                                    result.AlignedGray[roi.Y + y, roi.X + x] = ecc.gray[sy, sx];
                            }
                    }
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] parallel glyph ms="
                        + glyphWatch.ElapsedMilliseconds + ", workers=" + workerCount + ", globalMax=4");
                    results.Add(result);
                    SaveLargeADiagnostics(result, alignMode);
                    SaveGlyphRoiBoxes(result, alignMode);
                    Debug.WriteLine("[NativeFont][" + side + "][" + group + "] characters=" + result.Characters.Count + ", total elapsed ms=" + watch.ElapsedMilliseconds);
                }
                return results;
            }
        }
    }
}
