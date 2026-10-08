# REST API Designer & Integrated Execution Library (`API_Integarated`)

[ 🇬🇧 English ](README.md) • [ 🇻🇳 Tiếng Việt ](README_VN.md)

<p align="center">
  <img src="docs/images/banner.png" alt="REST API Studio & Code Generator Banner" width="100%" />
</p>

> Thư viện và công cụ thiết kế, kiểm thử và thực thi REST API theo hướng **UI-First** dành cho các ứng dụng .NET / C#.  
> Giúp loại bỏ hoàn toàn việc viết mã HTTP thủ công (`HttpClient`, ghép chuỗi URL, chèn Headers, xử lý Token, mã hóa Body, bắt lỗi SSL, parse JSON...) trong mọi dự án.

---

## 🎬 Video Minh Họa Trực Quan (Interactive Demo)

<p align="center">
  <img src="docs/images/demo.gif" alt="REST API Studio Live Demo" width="100%" />
</p>

---

## 📑 Mục Lục
1. [Giới thiệu & Lợi ích cốt lõi](#-giới-thiệu--lợi-ích-cốt-lõi)
2. [Cấu trúc Thư mục & Dự án](#-cấu-trúc-thư-mục--dự-án)
3. [Hướng dẫn Sử dụng Giao diện Thiết kế (UI Studio)](#-hướng-dẫn-sử-dụng-giao-diện-thiết-kế-ui-studio)
   - [3.1. Khởi động Tool](#31-khởi-động-tool)
   - [3.2. Tạo & Cấu hình Request](#32-tạo--cấu-hình-request)
   - [3.3. Cấu hình Xác thực (Authentication)](#33-cấu-hình-xác-thực-authentication)
   - [3.4. Soạn thảo Request Body](#34-soạn-thảo-request-body)
   - [3.5. Kiểm thử Thời gian thực (Test Runner)](#35-kiểm-thử-thời-gian-thực-test-runner)
   - [3.6. Tự động sinh mã Model & Code (C#, TypeScript, Python)](#36-tự-động-sinh-mã-model--code-c-typescript-python)
   - [3.7. Lưu & Mở File Cấu hình (.json)](#37-lưu--mở-file-cấu-hình-json)
4. [Hướng dẫn Tích hợp vào Dự án Khác](#-hướng-dẫn-tích-hợp-vào-dự-án-khác)
   - [Cách 1: Sử dụng ApiClient với C# Model (Khuyên dùng)](#cách-1-sử-dụng-apiclient-với-c-model-khuyên-dùng)
   - [Cách 2: Sử dụng Dữ liệu Động (Dynamic / JsonNode - Không cần tạo Class)](#cách-2-sử-dụng-dữ-liệu-động-dynamic--jsonnode---không-cần-tạo-class)
   - [Cách 3: Nhúng trực tiếp Giao diện vào Ứng dụng WPF (như R-Link)](#cách-3-nhúng-trực-tiếp-giao-diện-vào-ứng-dụng-wpf-như-r-link)
5. [Cơ chế Biến Môi trường & Biến Tự Động (Variables)](#-cơ-chế-biến-môi-trường--biến-tự-động-variables)
6. [Chạy Kiểm thử (Unit Tests)](#-chạy-kiểm-thử-unit-tests)
7. [Bản quyền & Tích hợp](#-bản-quyền--tích-hợp)

---

## 🌟 Giới thiệu & Lợi ích cốt lõi

### Quy trình làm việc truyền thống vs `API_Integarated`

```text
TRƯỚC ĐÂY (Thủ công):
[Đọc tài liệu API] -> [Bật Postman test] -> [Viết hàng chục dòng HttpClient C#] -> [Tự viết Class DTO] -> [Sửa code khi API đổi]

BÂY GIỜ (UI-First):
[Thiết kế & Test trên Tool] -> [Bấm Save JSON & Gen Code/Models] -> [Gọi API trong dự án chỉ với 1 dòng code]
```

* **Không cần code HTTP lặp lại**: Mọi cấu hình URL, Header, Query, Auth, Timeout, SSL Bypass đều nằm trong file cấu hình JSON.
* **Tự sinh mã đa ngôn ngữ**: Tự động phân tích cả Request Body và Response Body để sinh ra các Model và Client Code chất lượng cao trong **C#**, **TypeScript**, và **Python**.
* **Dùng được cho mọi dự án**: Từ Console App, Windows Service, Web API cho đến các ứng dụng Desktop WPF/WinForms.
* **Bảo trì không cần sửa code**: Khi bên thứ 3 thay đổi URL hoặc thêm Headers, bạn chỉ cần mở Tool sửa và Save lại file JSON, **code của project không cần compile lại**.

---

## 📁 Cấu trúc Thư mục & Dự án

```text
API_LIB/API_Integarated/
│
├── Core/                                  # Thư viện lõi độc lập (Class Library)
│   ├── Models/
│   │   ├── ApiEnums.cs                    # Enum: ApiMethod, ApiAuthType, ApiBodyType, ApiKeyLocation
│   │   ├── ApiParameter.cs                # Model tham số Key-Value (Query, Header, Form)
│   │   ├── ApiAuthDefinition.cs           # Cấu hình Bearer, Basic, ApiKey, Custom Header
│   │   ├── ApiRequestBodyDefinition.cs    # Cấu hình Body JSON, FormUrlEncoded, FormData, Raw
│   │   ├── ApiResponseData.cs             # Kết quả trả về (Status, ms, Headers, Body, JsonNode)
│   │   ├── ApiEndpointDefinition.cs       # Cấu hình toàn diện 1 Endpoint
│   │   └── ApiCollection.cs               # Nhóm Endpoints & Biến môi trường
│   └── Services/
│       ├── VariableResolver.cs            # Thay thế {{var}} và biến động ($guid, $timestamp...)
│       ├── JsonHelper.cs                  # Format Indented JSON, Validate, trích xuất JSONPath
│       ├── IApiExecutionEngine.cs         # Interface thực thi gọi HTTP
│       ├── HttpExecutionEngine.cs         # Engine thực thi HttpClient bất đồng bộ & SSL bypass
│       ├── CSharpModelGenerator.cs        # Engine sinh mã C# Model & Full Snippet
│       ├── TypeScriptCodeGenerator.cs    # Engine sinh TypeScript Interfaces & Fetch Client
│       ├── PythonCodeGenerator.cs        # Engine sinh Python Pydantic v2 Models & Requests Snippet
│       ├── ApiStorageService.cs           # Lưu & Đọc cấu hình từ file .json
│       └── ApiClient.cs                   # Lớp Client cấp cao để gọi API bằng 1 dòng code
│
├── UI/                                    # Giao diện WPF & MVVM (UserControl & Window)
│   ├── Controls/
│   │   └── BindableTextEditor.cs          # AvalonEdit hỗ trợ Two-Way Binding, tô màu C#/JS/Python
│   ├── Converters/
│   │   └── UiConverters.cs                # Converters cho Method (màu sắc), Status Code, Visibility
│   ├── ViewModels/
│   │   ├── ApiStudioViewModel.cs          # ViewModel chính điều khiển giao diện
│   │   ├── EndpointItemViewModel.cs       # ViewModel của từng Endpoint
│   │   └── ParameterItemViewModel.cs      # ViewModel cho từng dòng tham số
│   └── Views/
│       ├── ApiStudioControl.xaml(.cs)     # UserControl WPF có thể nhúng vào bất kỳ ứng dụng nào
│       └── ApiStudioWindow.xaml(.cs)      # Cửa sổ độc lập (Standalone Window)
│
├── Demo/                                  # Ứng dụng chạy thử giao diện Studio
│   └── API_Integarated.Demo.csproj
│
└── Tests/                                 # Bộ kiểm thử Unit Test (MSTest)
    └── API_Integarated.Tests/
```

---

## 🖥️ Hướng dẫn Sử dụng Giao diện Thiết kế (UI Studio)

### 3.1. Khởi động Tool
Tại thư mục `d:\Project_RLink\API_LIB\API_Integarated`, chạy lệnh:
```powershell
dotnet run --project Demo\API_Integarated.Demo.csproj
```
Cửa sổ **REST API Designer Studio** sẽ xuất hiện kèm sẵn các API mẫu (GET, POST JSON, PUT, DELETE).

---

### 3.2. Tạo & Cấu hình Request
1. Nhìn sang cột bên trái:
   * Bấm nút **[+ Request]** để thêm một endpoint mới.
   * Bấm **[Copy]** để nhân bản endpoint đang chọn.
   * Bấm **[Delete]** để xóa endpoint không dùng.
2. Tại thanh điều khiển trung tâm:
   * Chọn **HTTP Method**: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`, `OPTIONS`. (Huy hiệu màu sắc sẽ tự đổi tương ứng: Xanh lá cho GET, Cam cho POST, Xanh dương cho PUT, Đỏ cho DELETE).
   * Nhập **URL**: Hỗ trợ gắn biến môi trường, ví dụ: `{{baseUrl}}/api/v1/devices/{id}`.
3. Trong tab **Params (Query)**:
   * Bấm **[+ Add Parameter]** để thêm tham số URL (Query string).
   * Nhập `Key`, `Value`, `Description`. Có checkbox `Active` để bật/tắt từng tham số mà không cần xóa.

---

### 3.3. Cấu hình Xác thực (Authentication)
Chuyển sang tab **Authorization**, chọn loại xác thực trong ComboBox:
* **None**: Không dùng xác thực.
* **BearerToken**: Nhập chuỗi Token (hỗ trợ nhập `{{token}}`). Tool tự chèn header `Authorization: Bearer <token>`.
* **ApiKey**: Nhập tên Key (ví dụ `X-API-KEY`), giá trị Key và chọn vị trí gửi trong **Header** hoặc **QueryString**.
* **BasicAuth**: Nhập `Username` và `Password`. Tool sẽ tự động mã hóa Base64 khi gửi.
* **CustomHeader**: Nhập tên Header và giá trị tùy ý.

---

### 3.4. Soạn thảo Request Body
Chuyển sang tab **Body** (dành cho POST, PUT, PATCH):
* Chọn kiểu Body bằng Radio Button:
  * `none`: Không gửi body.
  * `JSON (application/json)`: Trình soạn thảo **AvalonEdit** có đánh số dòng, tô màu cú pháp.
    * Bấm nút **[ Format JSON ]** để tự động căn chỉnh khoảng cách, làm đẹp JSON.
    * Bấm nút **[ Gen Model ]** để tự động sinh ngay model cho Request và copy vào clipboard theo ngôn ngữ đang chọn.
  * `x-www-form-urlencoded`: Bảng Key-Value cho form gửi thông thường.
  * `form-data`: Bảng Key-Value hỗ trợ upload dữ liệu multipart.
  * `raw text`: Văn bản thô.

---

### 3.5. Kiểm thử Thời gian thực (Test Runner)
1. Bấm nút **[ SEND ]** (hoặc hủy bằng nút **[Cancel]** khi đang gửi).
2. Quan sát khung kết quả **Response** ở dưới:
   * **Huy hiệu mã trạng thái**: `200 OK` (Xanh lá), `400 Bad Request` (Cam), `500 Error` (Đỏ).
   * **Thời gian phản hồi**: Đo chính xác theo mili-giây (`Time: 42 ms`).
   * **Tab Pretty JSON**: Hiển thị JSON có tô màu cú pháp và thụt dòng đẹp mắt.
   * **Tab Raw Body**: Xem văn bản phản hồi gốc.
   * **Tab Headers**: Xem toàn bộ các Response Headers trả về từ server.
3. Trong tab **Settings**:
   * Cho phép điều chỉnh **Timeout (seconds)**.
   * Checkbox **Ignore SSL Certificate Errors**: Bỏ qua cảnh báo SSL cho các thiết bị công nghiệp dùng chứng chỉ tự ký (self-signed).

---

### 3.6. Tự động sinh mã Model & Code (C#, TypeScript, Python)
Sau khi gửi request và nhận kết quả JSON, chuyển sang tab **`Code Generator`** ở khung dưới:

<p align="center">
  <img src="docs/images/frame4_python.png" alt="Multi-Language Code Generator" width="100%" />
</p>

1. **Chọn Ngôn ngữ (Language)**:
   * **`C#`**: Sinh POCO Classes với `System.Text.Json.Serialization.JsonPropertyName`.
   * **`TypeScript`**: Sinh TypeScript `interface` / `type` chuẩn, tự động camelCase thuộc tính hoặc quote chuỗi có ký tự đặc biệt, sinh hàm `fetch` bất đồng bộ hoàn chỉnh.
   * **`Python`**: Sinh Pydantic v2 `BaseModel` với `Field(default=None, alias=...)`, tự động chuyển tên trường sang `snake_case`, xử lý tránh trùng từ khóa Python (`from_`, `class_`, `id_`), và sinh script gọi API bằng thư viện `requests` kèm `model_validate()`.

2. **Chọn Chế độ sinh mã (Mode)**:
   * **Response Model**: Sinh Data Contracts cho cấu trúc dữ liệu server trả về.
   * **Request Model**: Sinh Data Contracts cho Request Body (hỗ trợ cả JSON, `x-www-form-urlencoded`, và `form-data`).
   * **Full Code Snippet**: Sinh trọn vẹn cả Models lẫn hàm thực thi gọi API hoàn chỉnh có thể chạy được ngay!

3. **Tùy chỉnh Tên Model (Model Name)**: Đặt tên Root Model mong muốn (ví dụ: `DeviceItem`). Trình sinh mã sẽ tự động đặt tên các Model con hoặc tên hàm tương ứng (`executeDeviceItem`, `execute_device_item`).

4. **Copy & Xuất File (Export)**:
   * Nút **[Copy ...]**: Tự động đổi nhãn theo ngôn ngữ (`Copy C#`, `Copy TypeScript`, `Copy Python`) và sao chép trực tiếp vào Clipboard.
   * Nút **[Export ... File]**: Tự động lưu ra file `.cs`, `.ts`, hoặc `.py` với bộ lọc file và phần mở rộng chuẩn.
   * Trình biên soạn code tích hợp sẵn tính năng tô màu cú pháp theo thời gian thực (Syntax Highlighting) cho cả 3 ngôn ngữ.

> [!TIP]
> Trình tạo code của Tool tự động xử lý các trường dữ liệu có dấu cách như `"capacity GB"`, `"CPU model"` thành thuộc tính hợp lệ tương ứng với chuẩn từng ngôn ngữ (ví dụ C#: `CapacityGB` với `[JsonPropertyName]`, TypeScript: `"CPU model"?: string;`, Python: `cpu_model: Optional[str] = Field(..., alias="CPU model")`). Đồng thời quét và gộp (merge) thuộc tính của tất cả các phần tử trong danh sách JSON Array để không bao giờ bị thiếu trường!

---

### 3.7. Lưu & Mở File Cấu hình (.json)
* Bấm **[Save]**: Lưu toàn bộ danh sách API, headers, auth và biến môi trường thành file `.json` (ví dụ: `MyDevicesApi.json`).
* Bấm **[Load]**: Mở file `.json` đã lưu trước đó để tiếp tục chỉnh sửa hoặc kiểm thử.

---

## 💻 Hướng dẫn Tích hợp vào Dự án Khác

### Cách 1: Sử dụng ApiClient với C# Model (Khuyên dùng)
Trong bất kỳ dự án C# nào (Console, WPF, Worker Service, Web API), thêm tham chiếu đến `API_Integarated.dll`:

```csharp
using API_Integarated.Core.Services;
using YourProject.Models; // Namespace chứa các Model vừa export từ Tool

// 1. Tải file cấu hình JSON đã thiết kế từ Tool
var api = await ApiClient.LoadFromFileAsync("MyDevicesApi.json");

// 2. Chuẩn bị Request Payload (có Intellisense gợi ý code)
var requestData = new CreateDeviceRequest
{
    Name = "Apple iPad Air",
    Data = new CreateDeviceRequest_Data
    {
        Year = 2026,
        Price = 1199.99,
        CPUModel = "Apple M3"
    }
};

// 3. Thực thi gọi API chỉ với 1 dòng code:
var result = await api.ExecuteAsync<CreateDeviceResponse>("CreateDevice", requestData);

// 4. Sử dụng kết quả
Console.WriteLine($"Tạo thành công ID: {result?.Id} lúc: {result?.CreatedAt}");
```

---

### Cách 2: Sử dụng Dữ liệu Động (Dynamic / JsonNode - Không cần tạo Class)
Nếu bạn chỉ cần đọc nhanh 1 vài trường từ API mà không muốn mất công tạo class Model:

```csharp
using API_Integarated.Core.Services;

var api = await ApiClient.LoadFromFileAsync("MyDevicesApi.json");

// 1. Gọi API và lấy mảng JSON trực tiếp
var array = await api.ExecuteAsJsonArrayAsync("GetObjects");

// 2. Truy cập trực tiếp theo cấu trúc JSON
string? name = array?[0]?["name"]?.ToString();
string? color = array?[0]?["data"]?["color"]?.ToString();
string? cpu = array?[6]?["data"]?["CPU model"]?.ToString();

Console.WriteLine($"Thiết bị: {name} - Màu: {color} - CPU: {cpu}");
```

---

### Cách 3: Nhúng trực tiếp Giao diện vào Ứng dụng WPF (như R-Link)

#### Lựa chọn A: Mở cửa sổ Studio độc lập từ Menu / Nút bấm
```csharp
using API_Integarated.UI.Views;

private void OnOpenApiDesignerClick(object sender, RoutedEventArgs e)
{
    var studioWindow = new ApiStudioWindow();
    studioWindow.Owner = this;
    studioWindow.ShowDialog();
}
```

#### Lựa chọn B: Nhúng trực tiếp UserControl vào TabControl / View XAML
```xml
<UserControl xmlns:api="clr-namespace:API_Integarated.UI.Views;assembly=API_Integarated" ...>
    <Grid>
        <!-- Nhúng toàn bộ Studio vào giao diện của bạn -->
        <api:ApiStudioControl />
    </Grid>
</UserControl>
```

---

## ⚙️ Cơ chế Biến Môi trường & Biến Tự Động (Variables)

Bạn có thể sử dụng cú pháp `{{tên_biến}}` tại bất kỳ đâu: trong **URL**, **Query Parameters**, **Headers**, **Authentication Token**, và **Request Body**.

### 1. Biến Hệ thống Sinh Tự Động
| Cú pháp | Ý nghĩa | Ví dụ giá trị sinh ra |
|---|---|---|
| `{{$guid}}` | Sinh chuỗi UUID ngẫu nhiên v4 | `e2a4b834-9fc5-4c07-bf41-7e88241fa901` |
| `{{$timestamp}}` | UNIX timestamp hiện tại (giây) | `1728364800` |
| `{{$isoTimestamp}}` | Thời gian chuẩn ISO 8601 UTC | `2026-10-08T04:45:00.0000000Z` |
| `{{$randomInt}}` | Số nguyên ngẫu nhiên từ 1 - 100000 | `48291` |

### 2. Tự động trích xuất dữ liệu từ Response (Chaining API)
Trong tab **Response Extraction**, bạn có thể cấu hình quy tắc lấy dữ liệu từ API này để truyền sang API khác:
* **Target Variable**: `authToken`
* **JSON Path**: `$.data.token` hoặc `token`

Khi API thực thi thành công, Tool sẽ tự động trích xuất token và lưu vào biến môi trường `{{authToken}}` để các API tiếp theo tự động có token gọi tiếp.

---

## 🧪 Chạy Kiểm thử (Unit Tests)

Thư viện đi kèm bộ kiểm thử tự động toàn diện kiểm tra tất cả các chức năng Core Engine, JSON formatting, trích xuất dữ liệu, sinh code C#, TypeScript, Python, và `ApiClient`.

Chạy lệnh kiểm thử:
```powershell
dotnet test Tests\API_Integarated.Tests\API_Integarated.Tests.csproj
```

**Kết quả kiểm thử:**
* `VariableResolver_ShouldResolveCustomAndDynamicVariables`: **PASSED**
* `JsonHelper_ShouldFormatJsonAndExtractPath`: **PASSED**
* `ApiStorageService_ShouldSaveAndLoadCollection`: **PASSED**
* `HttpExecutionEngine_ShouldHandleLocalFailureGracefully`: **PASSED**
* `ApiClient_ShouldExecuteEndpointByNameAndInjectParams`: **PASSED**
* `CSharpModelGenerator_ShouldGenerateClassesForComplexJsonArray`: **PASSED**
* `CSharpModelGenerator_ShouldGenerateRequestModelFromEndpointBody`: **PASSED**
* `ApiResponseData_ShouldSupportDynamicJsonArrayParsingWithoutModel`: **PASSED**
* `TypeScriptCodeGenerator_ShouldGenerateInterfacesAndSnippet`: **PASSED**
* `TypeScriptCodeGenerator_ShouldHandleFormUrlEncodedAndFormDataRequests`: **PASSED**
* `PythonCodeGenerator_ShouldGeneratePydanticModelsAndSnippet`: **PASSED**
* `PythonCodeGenerator_ShouldHandleSnakeCaseAndPythonKeywords`: **PASSED**
* `ApiStudioViewModel_ShouldSupportLanguageSwitchingAndCodeGeneration`: **PASSED**

---

## 📄 Bản quyền & Tích hợp
Được phát triển và tối ưu cho hệ thống tích hợp **R-Link Ecosystem** (.NET 10 / WPF / C#).  
All rights reserved.
