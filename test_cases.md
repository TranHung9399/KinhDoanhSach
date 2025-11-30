# Test Cases - Website Bán Sách

## 1. Trang chủ (Home Page)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_HOME_01 | Kiểm tra hiển thị giao diện trang chủ | Truy cập vào trang web | N/A | 1. Mở trình duyệt<br>2. Nhập URL trang web | Trang chủ hiển thị đầy đủ Header, Footer, Banner, Danh sách sách nổi bật, Menu danh mục | | | | 29/11/2025 | AI Assistant |
| TC_HOME_02 | Kiểm tra chức năng tìm kiếm sách | Truy cập vào trang web | Từ khóa: "Conan" | 1. Nhập "Conan" vào ô tìm kiếm<br>2. Nhấn Enter hoặc icon tìm kiếm | Hiển thị danh sách sách có tên chứa "Conan" | | | | 29/11/2025 | AI Assistant |
| TC_HOME_03 | Kiểm tra liên kết đến trang chi tiết sách | Truy cập vào trang web | Sách ID: S001 | 1. Click vào hình ảnh hoặc tên sách bất kỳ | Chuyển hướng đến trang chi tiết của sách đó | | | | 29/11/2025 | AI Assistant |
| TC_HOME_04 | Tìm kiếm với từ khóa không có kết quả | Truy cập vào trang web | Từ khóa: "xyzabc123" | 1. Nhập từ khóa không tồn tại<br>2. Search | Hiển thị thông báo "Không tìm thấy sản phẩm nào" | | | | 29/11/2025 | AI Assistant |
| TC_HOME_05 | Tìm kiếm với ký tự đặc biệt | Truy cập vào trang web | Từ khóa: "@#$%" | 1. Nhập ký tự đặc biệt<br>2. Search | Hệ thống xử lý an toàn, không lỗi code, trả về kết quả rỗng hoặc phù hợp | | | | 29/11/2025 | AI Assistant |
| TC_HOME_06 | Kiểm tra phân trang (nếu có) | Trang chủ có > 10 sách | N/A | 1. Kéo xuống cuối trang<br>2. Nhấn trang 2 | Hiển thị danh sách sách trang 2, không trùng trang 1 | | | | 29/11/2025 | AI Assistant |

## 2. Đăng ký (Register)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_REG_01 | Đăng ký thành công với thông tin hợp lệ | Truy cập trang Đăng ký | HoTen: "Nguyen Van A"<br>Email: "nva@test.com"<br>Pass: "123456"<br>Phone: "0909123456" | 1. Nhập đầy đủ thông tin hợp lệ<br>2. Nhấn nút "Đăng ký" | Thông báo đăng ký thành công, chuyển hướng đến trang đăng nhập hoặc trang chủ | | | | 29/11/2025 | AI Assistant |
| TC_REG_02 | Đăng ký thất bại khi để trống trường bắt buộc | Truy cập trang Đăng ký | HoTen: ""<br>Email: "nva@test.com" | 1. Để trống "Họ tên"<br>2. Nhập các trường khác<br>3. Nhấn "Đăng ký" | Hiển thị thông báo lỗi yêu cầu nhập Họ tên | | | | 29/11/2025 | AI Assistant |
| TC_REG_03 | Đăng ký thất bại khi Email đã tồn tại | Truy cập trang Đăng ký | Email: "existing@test.com" (đã có trong DB) | 1. Nhập Email đã tồn tại<br>2. Nhập các trường khác hợp lệ<br>3. Nhấn "Đăng ký" | Hiển thị thông báo lỗi "Email đã được sử dụng" | | | | 29/11/2025 | AI Assistant |
| TC_REG_04 | Đăng ký thất bại khi Mật khẩu nhập lại không khớp | Truy cập trang Đăng ký | Pass: "123456"<br>ConfirmPass: "1234567" | 1. Nhập Mật khẩu và Nhập lại mật khẩu khác nhau<br>2. Nhấn "Đăng ký" | Hiển thị thông báo "Mật khẩu nhập lại không khớp" | | | | 29/11/2025 | AI Assistant |
| TC_REG_05 | Kiểm tra định dạng số điện thoại | Truy cập trang Đăng ký | Phone: "abc" hoặc "123" | 1. Nhập SĐT không phải số hoặc quá ngắn<br>2. Đăng ký | Hiển thị lỗi "Số điện thoại không hợp lệ" | | | | 29/11/2025 | AI Assistant |
| TC_REG_06 | Kiểm tra độ dài mật khẩu tối thiểu | Truy cập trang Đăng ký | Pass: "123" | 1. Nhập mật khẩu < 6 ký tự<br>2. Đăng ký | Hiển thị lỗi "Mật khẩu phải từ 6 ký tự trở lên" | | | | 29/11/2025 | AI Assistant |

## 3. Đăng nhập (Login)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_LOG_01 | Đăng nhập thành công | Truy cập trang Đăng nhập | Email: "user@test.com"<br>Pass: "123456" | 1. Nhập Email và Password đúng<br>2. Nhấn "Đăng nhập" | Đăng nhập thành công, chuyển hướng về trang chủ, hiển thị tên user trên Header | | | | 29/11/2025 | AI Assistant |
| TC_LOG_02 | Đăng nhập thất bại sai mật khẩu | Truy cập trang Đăng nhập | Email: "user@test.com"<br>Pass: "wrongpass" | 1. Nhập Email đúng, Pass sai<br>2. Nhấn "Đăng nhập" | Hiển thị thông báo lỗi "Sai thông tin đăng nhập" | | | | 29/11/2025 | AI Assistant |
| TC_LOG_03 | Đăng nhập thất bại email không tồn tại | Truy cập trang Đăng nhập | Email: "noexist@test.com" | 1. Nhập Email chưa đăng ký<br>2. Nhấn "Đăng nhập" | Hiển thị thông báo lỗi "Tài khoản không tồn tại" hoặc sai thông tin | | | | 29/11/2025 | AI Assistant |
| TC_LOG_04 | Đăng nhập thất bại khi tài khoản bị khóa | Truy cập trang Đăng nhập | Email: "locked@test.com" (TrangThai = 0/False) | 1. Nhập Email tài khoản bị khóa<br>2. Nhấn "Đăng nhập" | Hiển thị thông báo "Tài khoản của bạn đã bị khóa" | | | | 29/11/2025 | AI Assistant |
| TC_LOG_05 | Kiểm tra SQL Injection cơ bản | Truy cập trang Đăng nhập | Email: "' OR 1=1 --" | 1. Nhập payload SQL Injection vào email<br>2. Đăng nhập | Hệ thống không bị bypass, báo lỗi đăng nhập sai | | | | 29/11/2025 | AI Assistant |
| TC_LOG_06 | Kiểm tra khoảng trắng thừa | Truy cập trang Đăng nhập | Email: " user@test.com " | 1. Nhập email có khoảng trắng đầu/cuối<br>2. Đăng nhập | Hệ thống tự trim khoảng trắng và đăng nhập thành công | | | | 29/11/2025 | AI Assistant |

## 4. Quên mật khẩu (Forgot Password)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_FORGOT_01 | Yêu cầu lấy lại mật khẩu thành công | Truy cập trang Quên mật khẩu | Email: "user@test.com" | 1. Nhập Email đã đăng ký<br>2. Nhấn "Gửi yêu cầu" | Thông báo đã gửi hướng dẫn/OTP về email | | | | 29/11/2025 | AI Assistant |
| TC_FORGOT_02 | Yêu cầu thất bại với email chưa đăng ký | Truy cập trang Quên mật khẩu | Email: "new@test.com" | 1. Nhập Email chưa đăng ký<br>2. Nhấn "Gửi yêu cầu" | Thông báo lỗi "Email không tồn tại trong hệ thống" | | | | 29/11/2025 | AI Assistant |

## 5. Quản lý thông tin cá nhân (Profile)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_PROF_01 | Cập nhật thông tin cá nhân thành công | Đã đăng nhập | HoTen: "Nguyen Van B"<br>SDT: "0987654321" | 1. Vào trang cá nhân<br>2. Sửa Họ tên, SĐT<br>3. Nhấn "Lưu" | Thông báo cập nhật thành công, dữ liệu hiển thị đúng sau khi load lại | | | | 29/11/2025 | AI Assistant |
| TC_PROF_02 | Đổi mật khẩu thành công | Đã đăng nhập | OldPass: "123456"<br>NewPass: "654321" | 1. Vào tab Đổi mật khẩu<br>2. Nhập mật khẩu cũ, mới<br>3. Nhấn "Đổi mật khẩu" | Thông báo đổi mật khẩu thành công | | | | 29/11/2025 | AI Assistant |
| TC_PROF_03 | Đổi mật khẩu thất bại do sai mật khẩu cũ | Đã đăng nhập | OldPass: "wrong"<br>NewPass: "654321" | 1. Nhập sai mật khẩu cũ<br>2. Lưu | Báo lỗi "Mật khẩu cũ không chính xác" | | | | 29/11/2025 | AI Assistant |
| TC_PROF_04 | Đổi mật khẩu thất bại do mật khẩu mới trùng mật khẩu cũ | Đã đăng nhập | Old: "123"<br>New: "123" | 1. Nhập mật khẩu mới giống cũ<br>2. Lưu | Cảnh báo "Mật khẩu mới không được trùng mật khẩu cũ" (nếu có rule này) | | | | 29/11/2025 | AI Assistant |

## 6. Quản lý nhân viên và chức vụ (Employee & Role)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_EMP_01 | Thêm mới nhân viên thành công | Login Admin | HoTen: "NV Moi"<br>Email: "nv@store.com"<br>ChucVu: "BanHang" | 1. Vào Quản lý nhân viên<br>2. Nhấn "Thêm mới"<br>3. Nhập thông tin<br>4. Nhấn "Lưu" | Nhân viên mới hiển thị trong danh sách, dữ liệu lưu đúng vào DB | | | | 29/11/2025 | AI Assistant |
| TC_EMP_02 | Cập nhật chức vụ nhân viên | Login Admin | NV: "NV001"<br>NewChucVu: "QuanLy" | 1. Chọn nhân viên cần sửa<br>2. Đổi chức vụ<br>3. Nhấn "Lưu" | Chức vụ nhân viên thay đổi thành công | | | | 29/11/2025 | AI Assistant |
| TC_EMP_03 | Báo lỗi khi thêm nhân viên trùng tên tài khoản | Login Admin | TenTK: "admin" (đã tồn tại) | 1. Nhập Tên tài khoản đã tồn tại<br>2. Nhấn "Lưu" | Hiển thị thông báo "Tên tài khoản đã tồn tại" | | | | 29/11/2025 | AI Assistant |
| TC_EMP_04 | Xóa nhân viên (Soft Delete) | Login Admin | NV: "NV_NghiViec" | 1. Chọn xóa nhân viên<br>2. Xác nhận | Trạng thái nhân viên chuyển sang "Ngưng hoạt động", không mất data | | | | 29/11/2025 | AI Assistant |
| TC_EMP_05 | Chặn xóa tài khoản đang đăng nhập | Login Admin (NV01) | NV: "NV01" | 1. Admin tự chọn xóa chính mình<br>2. Xác nhận | Hệ thống chặn, báo lỗi "Không thể xóa tài khoản đang đăng nhập" | | | | 29/11/2025 | AI Assistant |
| TC_ROLE_01 | Thêm mới chức vụ | Login Admin | TenCV: "Shipper" | 1. Vào Quản lý chức vụ<br>2. Thêm chức vụ mới<br>3. Lưu | Chức vụ mới xuất hiện trong danh sách chọn | | | | 29/11/2025 | AI Assistant |

## 7. Quản lý sách (Book Management)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_BOOK_01 | Thêm sách mới thành công | Login Admin | TenSach: "Dế Mèn"<br>Gia: 50000<br>SoLuong: 100 | 1. Vào Quản lý sách<br>2. Nhấn "Thêm"<br>3. Nhập đầy đủ thông tin<br>4. Upload hình<br>5. Lưu | Sách mới hiển thị trong danh sách quản lý và trang chủ | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_02 | Báo lỗi khi nhập giá âm | Login Admin | Gia: -10000 | 1. Nhập giá bán là số âm<br>2. Lưu | Hiển thị thông báo lỗi "Giá bán phải lớn hơn 0" | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_03 | Báo lỗi khi giá chiết khấu cao hơn giá bán | Login Admin | GiaBan: 100k<br>GiaChietKhau: 120k | 1. Nhập Giá chiết khấu > Giá bán<br>2. Lưu | Hiển thị lỗi "Giá chiết khấu không được lớn hơn giá bán" | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_04 | Xóa sách (hoặc ẩn sách) | Login Admin | SachID: "S001" | 1. Chọn sách cần xóa<br>2. Nhấn "Xóa"<br>3. Xác nhận | Sách không còn hiển thị trên trang bán hàng (Status = 0 hoặc xóa hẳn) | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_05 | Upload ảnh không đúng định dạng | Login Admin | File: "test.txt" hoặc "test.exe" | 1. Chọn file không phải ảnh ở mục Hình ảnh<br>2. Lưu | Báo lỗi "Chỉ chấp nhận file ảnh (jpg, png...)" | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_06 | Kiểm tra tồn kho khi hết hàng | Login Admin | SoLuong: 0 | 1. Sửa số lượng tồn về 0<br>2. Ra trang chủ xem sách đó | Hiển thị trạng thái "Hết hàng", không cho thêm vào giỏ | | | | 29/11/2025 | AI Assistant |
| TC_BOOK_07 | Thêm sách trùng Mã Sách | Login Admin | MaSach: "S001" (đã có) | 1. Nhập Mã sách đã tồn tại<br>2. Lưu | Báo lỗi "Mã sách đã tồn tại" | | | | 29/11/2025 | AI Assistant |


## 8. Quản lý danh mục (Category)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_CAT_01 | Thêm danh mục mới | Login Admin | TenLoai: "Truyện Tranh" | 1. Vào Quản lý danh mục<br>2. Thêm mới<br>3. Nhập tên loại<br>4. Lưu | Danh mục mới được tạo thành công | | | | 29/11/2025 | AI Assistant |
| TC_CAT_02 | Sửa tên danh mục | Login Admin | Old: "Truyện"<br>New: "Tiểu thuyết" | 1. Chọn danh mục<br>2. Sửa tên<br>3. Lưu | Tên danh mục cập nhật thành công | | | | 29/11/2025 | AI Assistant |
| TC_CAT_03 | Xóa danh mục đang chứa sách | Login Admin | Danh mục "A" có 5 sách | 1. Chọn xóa danh mục đang có sách<br>2. Xác nhận | Hiển thị cảnh báo "Không thể xóa danh mục đang chứa sách" hoặc yêu cầu chuyển sách sang danh mục khác | | | | 29/11/2025 | AI Assistant |
| TC_CAT_04 | Thêm danh mục trùng tên | Login Admin | TenLoai: "Truyện" (đã có) | 1. Nhập tên danh mục đã tồn tại<br>2. Lưu | Báo lỗi "Tên danh mục đã tồn tại" | | | | 29/11/2025 | AI Assistant |

## 9. Quản lý khách hàng (Customer Management)

| TC ID | Summary | Pre-condition | Test Data | Steps | Expected Result | Result | Bug # | Notes | Ngày tháng | Tester |
|---|---|---|---|---|---|---|---|---|---|---|
| TC_CUST_01 | Xem danh sách khách hàng | Login Admin | N/A | 1. Vào menu Quản lý khách hàng | Hiển thị danh sách khách hàng với các cột: Mã KH, Họ tên, Email, SĐT | | | | 29/11/2025 | AI Assistant |
| TC_CUST_02 | Tìm kiếm khách hàng | Login Admin | Keyword: "0909..." | 1. Nhập SĐT vào ô tìm kiếm<br>2. Search | Hiển thị khách hàng có SĐT tương ứng | | | | 29/11/2025 | AI Assistant |
| TC_CUST_03 | Khóa tài khoản khách hàng | Login Admin | KH: "KH001" | 1. Chọn khách hàng<br>2. Đổi trạng thái sang "Khóa"<br>3. Lưu | Khách hàng không thể đăng nhập được nữa | | | | 29/11/2025 | AI Assistant |
| TC_CUST_04 | Mở khóa tài khoản khách hàng | Login Admin | KH: "KH001" (đang khóa) | 1. Chọn khách hàng<br>2. Đổi trạng thái sang "Hoạt động"<br>3. Lưu | Khách hàng đăng nhập lại bình thường | | | | 29/11/2025 | AI Assistant |
| TC_CUST_05 | Kiểm tra lịch sử mua hàng của khách | Login Admin | KH: "KH001" | 1. Chọn xem chi tiết khách hàng<br>2. Xem tab Lịch sử đơn hàng | Hiển thị danh sách các đơn hàng khách đã đặt | | | | 29/11/2025 | AI Assistant |
