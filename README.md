# Black Myth: Wukong — ASP.NET MVC 5 Study Project

Bản chuyển đổi từ trang tĩnh/Node.js sang **ASP.NET Framework MVC 5** hoàn chỉnh, giữ nguyên giao diện điện ảnh của trang gốc (năm màn hình toàn khung nhìn, carousel, modal tài khoản, luồng mua hàng) và bổ sung cơ sở dữ liệu SQL Server 12 bảng, Entity Framework 6 Database First, xác thực Forms Authentication, phân quyền Admin/User và đầy đủ CRUD.

> Đây là project phục vụ học tập, không phải sản phẩm chính thức của Game Science.

## 1. Công nghệ

| Thành phần | Phiên bản |
| --- | --- |
| .NET Framework | 4.8 |
| ASP.NET MVC | 5.2.9 |
| Entity Framework | 6.4.4 (Database First, ObjectContext) |
| SQL Server | 2016+ (đã kiểm thử trên SQL Server 2022) |
| Bootstrap | 5.2.3 (trang catalogue, tài khoản, quản trị) |
| jQuery | 3.7.0 + jQuery Validation (unobtrusive) |
| Giao diện landing page | HTML/CSS/JS thuần, giữ nguyên từ bản gốc |

Toàn bộ thư viện được tham chiếu cục bộ trong `packages/`, không cần Internet khi build.

## 2. Yêu cầu môi trường

- Windows 10/11.
- Visual Studio 2019/2022/2026 có workload **ASP.NET and web development** (hoặc MSBuild + IIS Express).
- SQL Server Database Engine đang chạy, đăng nhập bằng Windows Authentication.
- Quyền tạo database trên instance SQL Server.

## 3. Tạo cơ sở dữ liệu

Mở `database/setup.sql` bằng SQL Server Management Studio và **Execute**, hoặc chạy:

```powershell
sqlcmd -S localhost -E -i database\setup.sql
```

Script sẽ:

1. Tạo database `WukongStudy` nếu chưa có.
2. Xóa và tạo lại **12 bảng** theo đúng thứ tự khóa ngoại.
3. Thêm dữ liệu mẫu: 2 vai trò, 2 tài khoản, 3 thể loại, 3 game, 4 phiên bản, media, giải thưởng và đánh giá.

Kết quả cuối script in ra `DatabaseReady`, `TableCount`, `UserCount`, `GameCount`. `TableCount` phải bằng **12**.

Nếu SQL Server của bạn dùng instance khác, sửa chuỗi kết nối trong `Web.config`:

```xml
<add name="WukongEntities"
     connectionString="metadata=res://*/Models.WukongModel.csdl|res://*/Models.WukongModel.ssdl|res://*/Models.WukongModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=localhost;initial catalog=WukongStudy;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;"
     providerName="System.Data.EntityClient" />
```

Ví dụ đổi `data source=localhost` thành `data source=localhost\SQLEXPRESS`.

## 4. Build

### Bằng Visual Studio

1. Mở `WukongWeb.sln`.
2. Chọn cấu hình `Debug`, nhấn **Build Solution**.
3. Nhấn **F5** (IIS Express) để chạy.

### Bằng dòng lệnh

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  WukongWeb.csproj /p:Configuration=Debug /v:minimal
```

Đường dẫn MSBuild phụ thuộc phiên bản Visual Studio đã cài (`2019`, `2022`, …). Có thể mở **Developer Command Prompt for VS** rồi chạy `msbuild WukongWeb.csproj` để không phải nhập đường dẫn đầy đủ.

### Chạy bằng IIS Express

```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" `
  /path:"C:\Users\Admin\Downloads\THVV\wukong-web-recreation" /port:54321 /clr:v4.0 /systray:false
```

Mở `http://localhost:54321/`.

## 5. Tài khoản demo

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Admin | `admin@wukong.local` | `Admin@12345` |
| User | `user@wukong.local` | `User@12345` |

Mật khẩu được lưu dưới dạng **PBKDF2-HMAC-SHA256, 100.000 vòng lặp**, salt ngẫu nhiên 128-bit cho từng tài khoản. Không có mật khẩu dạng plain text trong database.

## 6. Chức năng

### Trang công khai

- **Landing page** (`/`): giữ nguyên năm màn hình Hero → Awards → Overview → Media → Footer, điều hướng bằng bánh xe/vuốt/phím, carousel tự chạy 5 giây, menu ngôn ngữ, cookie notice, modal đăng nhập/đăng ký và popup chọn mua. Footer và modal tài khoản có liên kết nhanh tới Catalogue, Wishlist, My account; tài khoản Admin thấy thêm **Admin console**.
- **Catalogue** (`/Games`): tìm kiếm theo tên/mô tả, lọc theo thể loại, sắp xếp theo giá/ngày phát hành, phân trang.
- **Chi tiết game** (`/Games/Details/{id}`): thông tin, bảng phiên bản (mỗi phiên bản có nút mua riêng), điểm trung bình, danh sách đánh giá, form đánh giá và nút wishlist.
- **Wishlist** (`/Games/Wishlist`): danh sách game đã lưu của người dùng.
- **Tài khoản**: đăng nhập, đăng ký, hồ sơ, đổi mật khẩu, lịch sử đơn hàng.
- **Checkout** (`/Checkout`): xác nhận thanh toán demo, tạo đơn trong SQL Server, hiển thị hoá đơn (`/Checkout/Receipt/{id}`). Nhận `?gameId=` (lấy phiên bản rẻ nhất) hoặc `?editionId=` (mua đúng phiên bản đã chọn); không có tham số thì giữ hành vi cũ của nút Purchase ở landing page. Giá luôn đọc lại từ bảng `Editions`, không nhận giá từ trình duyệt.
- **Newsletter**: nút *Subscribe now* ở footer mở bảng nhập email, ghi vào bảng `NewsletterSubscribers`; email trùng được nhận diện và không tạo bản ghi lặp.
- **Trang 404**: mọi URL không tồn tại (hoặc bản ghi đã bị xóa) trả về trang lỗi 404 đúng giao diện kèm liên kết điều hướng, không phải màn hình lỗi mặc định của IIS.

### Khu quản trị (`/Admin`, chỉ vai trò Admin)

- Dashboard: tổng tài khoản, game, đơn hàng, doanh thu, top game bán chạy, đơn gần đây và danh sách email đăng ký newsletter.
- **Games**: danh sách, tìm kiếm, phân trang, tạo, sửa, xóa (hoặc ngừng kích hoạt nếu đã có lịch sử bán).
- **Orders**: danh sách đơn, tìm theo email/tên/trạng thái, đổi trạng thái (Pending/Paid/Cancelled/Refunded) và mở hoá đơn của khách.
- **Accounts**: tạo, sửa, bật/tắt, xóa tài khoản; gán vai trò Admin/User; chặn tự hạ quyền hoặc tự xóa chính mình.
- **Reviews**: duyệt và xóa đánh giá.

### Bảo mật

- Forms Authentication + ticket `UserData` chứa `role|displayName`, được giải mã trong `Application_PostAuthenticateRequest` để `[Authorize(Roles = "Admin")]` hoạt động ở tầng máy chủ.
- `[ValidateAntiForgeryToken]` trên mọi POST thay đổi dữ liệu; landing page dùng token trong form ẩn `#csrf-form` và tự làm mới token sau khi đăng nhập/đăng xuất qua AJAX.
- Giá đơn hàng lấy từ `Editions.Price` trong database, không nhận từ trình duyệt.
- Tạo đơn hàng trong `TransactionScope` để `Orders` và `OrderDetails` luôn nhất quán.
- Kiểm tra quyền sở hữu khi xem hoá đơn: người dùng chỉ xem được đơn của mình.

## 7. Kiến trúc thư mục

```
WukongWeb.sln
WukongWeb.csproj
Web.config                     Chuỗi kết nối, Forms Auth, binding redirect
Global.asax / Global.asax.cs   Khởi động ứng dụng, gắn role vào principal
App_Start/
  RouteConfig.cs               Định tuyến mặc định {controller}/{action}/{id}
  FilterConfig.cs              Bộ lọc toàn cục
  BundleConfig.cs              Đăng ký bundle (không dùng ở runtime)
Controllers/
  HomeController.cs            Landing page, dữ liệu awards/media
  AccountController.cs         Đăng nhập, đăng ký, hồ sơ, JSON cho modal
  GamesController.cs           Catalogue, chi tiết, đánh giá, wishlist
  CheckoutController.cs        Đặt hàng demo, hoá đơn
  AdminController.cs           CRUD game/account/review, dashboard
Infrastructure/
  BaseController.cs            Db context + CurrentUser dùng chung
  AuthService.cs               Ghi/xóa ticket Forms Authentication
  PasswordHasher.cs            PBKDF2-HMAC-SHA256
  PagedResult.cs               Kiểu phân trang dùng chung
Models/
  WukongModel.edmx             Mô hình Database First
  WukongModel.Designer.cs      ObjectContext + entity sinh tự động
  ViewModels/                  DTO cho từng màn hình, chứa Data Annotations
Views/
  Home/Index.cshtml            Landing page (giữ nguyên bố cục gốc)
  Games/, Checkout/, Account/, Admin/
  Shared/_Layout.cshtml        Layout landing page
  Shared/_PublicLayout.cshtml  Layout Bootstrap cho trang công khai
  Shared/_AdminLayout.cshtml   Layout khu quản trị
  Shared/_CheckoutLayout.cshtml
Content/
  styles.css                   CSS landing page gốc
  checkout.css                 CSS trang checkout gốc
  bootstrap.min.css
  site-admin.css               Bổ sung cho các trang MVC mới
Scripts/
  app.js                       Điều hướng landing + gọi API tài khoản
  checkout.js                  Gọi /Checkout/PlaceOrder
  jquery, bootstrap, validation
database/setup.sql             Tạo 12 bảng + dữ liệu mẫu
docs/ERD.md                    Sơ đồ ERD và giải thích quan hệ
_legacy/                       Bản HTML/Node.js gốc, chỉ lưu tham khảo
assets/, media/                Hình ảnh, font, video
```

## 8. Đối chiếu tiêu chí chấm điểm

| Tiêu chí | Đáp ứng |
| --- | --- |
| **A. HTML/CSS/JS/Bootstrap/Responsive (1.75)** | Landing page giữ nguyên HTML/CSS/JS gốc, có carousel, modal, hiệu ứng chuyển màn hình, `prefers-reduced-motion`, responsive theo breakpoint. Các trang mới dùng Bootstrap 5 grid, navbar, card, table, form, pagination. |
| **B. CRUD + xác thực/phân quyền + quản lý người dùng (2.00)** | CRUD đầy đủ cho Games, Accounts, Reviews; đơn hàng có tạo và xem. Đăng ký/đăng nhập/đăng xuất, đổi mật khẩu. `[Authorize(Roles="Admin")]` chặn toàn bộ `/Admin`. Admin tạo/sửa/bật-tắt/xóa tài khoản và gán vai trò. |
| **C. ASP.NET MVC (0.75)** | Controller → ViewModel → View, Razor, routing, `Html.BeginForm`, `Html.ValidationMessageFor`, partial view, layout, `TempData`, `ModelState`. |
| **D. ≥ 8 bảng + EF Database First (1.00)** | 12 bảng trong `database/setup.sql`; EDMX sinh từ database, nhúng CSDL/SSDL/MSL vào assembly; `ObjectContext` + `ObjectSet<T>`; quan hệ 1-n và nhiều-nhiều. |
| **E. Data Annotation + LINQ (0.50)** | ViewModel dùng `[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`, `[Compare]`, `[Display]`, `[DataType]`. Truy vấn LINQ lọc, sắp xếp, phân trang, `GroupBy`, `Sum`, `Average`, `Any`, `Count`. |
| **F. Chức năng chạy thực tế (1.00)** | Toàn bộ luồng chạy trên IIS Express + SQL Server: đăng ký → đăng nhập → xem catalogue → thêm wishlist → đánh giá → checkout → hoá đơn → quản trị CRUD. |

## 9. Kiểm thử nhanh

1. Mở `/`, cuộn qua năm màn hình, mở modal đăng nhập bằng `user@wukong.local` / `User@12345`.
2. Nhấn **BUY NOW → Buy on web**, xác nhận checkout, xem hoá đơn.
3. Vào `/Games`, tìm kiếm, lọc, mở chi tiết, gửi đánh giá, thêm wishlist.
4. Đăng nhập bằng `admin@wukong.local` / `Admin@12345`, mở `/Admin`.
5. Thử tạo/sửa/xóa game và tài khoản; kiểm tra tài khoản User không vào được `/Admin`.

## 10. Ghi chú kỹ thuật

- **Không dùng `Scripts.Render` / `Styles.Render`**: AjaxMin trong `System.Web.Optimization` không phân tích được cú pháp Bootstrap 5.2.3, nên các layout mới nhúng trực tiếp `<link>` và `<script>`.
- **Razor và UTF-8**: các tệp `.cshtml` được lưu dạng UTF-8 **có BOM** để tiếng Việt và ký tự `★`/`☆` hiển thị đúng.
- **EDMX và EF6**: mã sinh tự động đã được chuyển sang namespace EF6 (`System.Data.Entity.Core.*`); `WukongModel.Designer.cs` được biên dịch cùng project và ba tệp metadata được nhúng qua target `EntityDeploy`.
- **`_legacy/`**: chứa `index.html`, `checkout.html`, CSS/JS và Node.js server cũ để đối chiếu; không tham gia build.
- **Không có thanh toán thật**: checkout chỉ ghi đơn demo, không kết nối cổng thanh toán và không yêu cầu thông tin thẻ.

## 11. Xử lý sự cố

| Hiện tượng | Cách khắc phục |
| --- | --- |
| `Cannot open database "WukongStudy"` | Chạy lại `database/setup.sql`; kiểm tra chuỗi kết nối trong `Web.config`. |
| `The model backing the context has changed` | EDMX và database không khớp: chạy lại script tạo database, sau đó rebuild. |
| Trang báo 500 khi mở view | Bật `customErrors="Off"` trong `Web.config` (đang bật sẵn ở môi trường học tập) và xem chi tiết lỗi. |
| Không đăng nhập được | Kiểm tra tài khoản còn `IsActive = 1`; dùng đúng email/mật khẩu demo. |
| CSS landing page mất hình nền | Đảm bảo thư mục `assets/` nằm cạnh `Web.config` và chạy qua IIS/IIS Express, không mở bằng `file://`. |
#   b l a c k m y t h _ w u k o n g  
 