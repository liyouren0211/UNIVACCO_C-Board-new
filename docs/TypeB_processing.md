# 目錄
[主流程](#主流程)

[ROI](#ROI)

[切面分析](#切面分析)

[飽滿度](#飽滿度)

[螺旋區](#螺旋區)

[陰陽版](#陰陽版)

## 主流程

[回到目錄](#目錄)

## ROI

```mermaid
	flowchart TD
		輸入原圖
		--> 判斷是否需要旋轉180度?{判斷是否需要旋轉180度?}
		判斷是否需要旋轉180度? -->|是| 執行旋轉
		判斷是否需要旋轉180度? -->|否| 判斷是否為冷燙版?{判斷是否為冷燙版?}
		執行旋轉 --> 判斷是否為冷燙版?
		判斷是否為冷燙版? -->|是| 膨脹
		判斷是否為冷燙版? -->|否| 找輪廓
		膨脹 --> 找輪廓
		--> 篩選最大面積以外的輪廓製作遮罩
		--> 遮罩與原圖相乘留下邊框
		--> 霍夫直線轉換
		--> 邊框左上角與右上角點計算斜率
		--> 旋轉原圖與旋轉霍夫線
		--> 霍夫線計算四個角落的點
		--> 裁切原圖
		--> 輸出ROI
		classDef default fill:transparent,stroke:#000000,stroke-width:2px;
```


| 流程圖                     | 圖片                      |
|----------------------------|---------------------------|
|輸入原圖| ![](../assets/hough_rotate1_sourceimg.png)|
|執行旋轉|![](../assets/hough_rotate2_flip.png)|
|膨脹|![](../assets/hough_rotate5_dilateimg.png) |
|找輪廓並製作遮罩|![](../assets/hough_rotate6_mask.png) |
|遮罩與原圖相乘留下邊框|![](../assets/hough_rotate7_maskand.png) |
|霍夫直線轉換|![](../assets/hough_rotate8_houghimg.png) |
|旋轉原圖|![](../assets/hough_rotate9_rot.png) |
|輸出ROI|![](../assets/hough_rotate11_roiframe.png) |

[回到目錄](#目錄)

## 切面分析
這是切面分析的內容



[回到目錄](#目錄)

## 飽滿度
這是飽滿度的內容。
```mermaid
	flowchart TD
		輸入ROI圖
		--> 絕對位置裁切飽和區
		--> 計算黑色素與總面積占比
		--> 依百分比判定分數區間
		--> 輸出結果至UI
	
		classDef default fill:transparent,stroke:#000000,stroke-width:2px;
```
[回到目錄](#目錄)

## 螺旋區
這是螺旋區的內容。
```mermaid
	flowchart TD
		輸入ROI圖
		--> 絕對位置裁切飽和區
		--> 以中心點製作圓形遮罩
		--> 與原圖相加
		--> 計算黑色素與總面積占比
		--> 輸出百分比結果至UI
	
		classDef default fill:transparent,stroke:#000000,stroke-width:2px;
```
[回到目錄](#目錄)

## 陰陽版
這是陰陽版的內容。
```mermaid
	flowchart TD
		輸入ROI圖
		--> 裁切出左半邊圖
		--> 找最大面積輪廓
		--> 連通區域分析法
		--> 依據各絕對位置分類各連通區域標註
		--> 依連通區域找出貼齊字體的位置
		

		
		
		
		
		classDef default fill:transparent,stroke:#000000,stroke-width:2px;
```
[回到目錄](#目錄)
