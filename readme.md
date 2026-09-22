
## H2
### H3
#### H4
##### H5
###### H6
** 兩個星號 **

### 文件導航測試
- [前往操作手冊](docs/OperationManual.md)
- [前往冷燙B版演算法介紹](docs/TypeB_processing.md)

### 流程圖測試

以下使用 Mermaid 語法製作的簡單流程圖：

```mermaid
flowchart TD
    Start([開始]) --> Decision{條件是否成立?}
    Decision -->|是| Action[執行操作]
    Decision -->|否| End([結束])
    Action --> End
