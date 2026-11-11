# Hướng dẫn vibe và hệ thống thiết kế

Tài liệu này mô tả phong cách hình ảnh của project để đưa vào prompt cho AI khi thiết kế thêm trang, section hoặc component. Hãy xem đây là bộ quy tắc định hướng: nội dung mới có thể sáng tạo, nhưng cần giữ cùng ngôn ngữ thị giác.

## 1. Tinh thần thiết kế

- **Từ khóa:** huyền bí, cổ phong, thần thoại phương Đông, điện ảnh, trầm mặc, mạnh mẽ, cao cấp.
- **Cảm giác:** như đang bước vào một thế giới thần thoại lâu đời; có chiều sâu, bí ẩn và hơi nguy hiểm, nhưng vẫn tinh tế.
- **Nhịp thị giác:** tương phản rõ giữa nền tối và chữ sáng; nhiều khoảng thở; ít chi tiết nhưng mỗi chi tiết có chủ đích.
- **Tham chiếu chất liệu:** mực đen, giấy ngà đã cũ, đồng hun, đá tối, khói, bụi và ánh lửa.
- Thiết kế hướng tới trải nghiệm game phiêu lưu hành động, không giống dashboard, cửa hàng công nghệ hay giao diện SaaS thông thường.

## 2. Bảng màu

| Vai trò | Tên gợi nhớ | Mã màu | Cách sử dụng |
|---|---|---|---|
| Nền chính | Đen mực | `#080807` | Nền toàn trang, header, vùng trống |
| Nền phụ | Đen than | `#10100E` | Card, panel, lớp nền nội dung |
| Chữ chính | Giấy ngà | `#E8DFCC` | Tiêu đề, nội dung quan trọng, icon |
| Nhấn phụ | Vàng đồng | `#C9A66E` | Viền, đường phân cách, nhãn nhỏ |
| Nhấn chính | Vàng giấy | `#E7C98E` | Nút hoặc trạng thái đang chọn, con số nổi bật |
| Nhấn hiếm | Đỏ gạch | `#8E3028` | Từ khóa, con dấu, một vài chi tiết huyền bí |
| Chữ phụ | Xám khói | `#9A9994` | Mô tả phụ, metadata, bản quyền |
| Đường mảnh | Ngà trong suốt | `rgba(232, 223, 204, 0.22)` | Viền nhẹ, đường phân đoạn |

### Tỷ lệ màu gợi ý

- Khoảng **70–80%** nền đen và đen than.
- Khoảng **15–25%** chữ ngà, ảnh sáng và vùng nội dung dễ đọc.
- Dùng vàng đồng/vàng giấy ở mức **điểm nhấn**, không phủ kín giao diện.
- Đỏ gạch chỉ nên xuất hiện rất ít.

> Tỷ lệ trên là gợi ý để giữ cảm giác tối và có chiều sâu, không phải con số bắt buộc.

## 3. Chữ và phân cấp

- **Tiêu đề lớn:** serif giàu cá tính, ưu tiên Crimson Pro hoặc Georgia; dùng chữ hoa hoặc cụm từ ngắn; cỡ lớn, nét thanh, khoảng cách chữ hơi âm.
- **Nội dung mô tả:** sans-serif dễ đọc như Arial/Helvetica; màu ngà dịu hoặc xám khói.
- **Eyebrow/nhãn:** sans-serif nhỏ, chữ hoa, tracking rộng, màu vàng sáng hoặc vàng đồng.
- **Số liệu:** serif màu vàng giấy, kích thước lớn; nhãn nguồn nhỏ hơn và dùng xám khói.
- Dùng tối đa hai họ chữ chính. Không dùng font sci-fi, geometric quá hiện đại, hoặc nhiều kiểu chữ trang trí cùng lúc.

## 4. Hình ảnh và chất liệu

- Chọn hình có ánh sáng tương phản mạnh, vùng tối sâu và một điểm sáng rõ.
- Ưu tiên tông đất, khói, đỏ trầm, vàng lửa và màu đá; giảm saturation nếu ảnh quá rực.
- Có thể phủ gradient đen lên ảnh để giữ độ tương phản cho chữ.
- Texture nên mờ và tinh tế: giấy, mực, bụi, khói, vân đá hoặc hạt phim. Không để texture gây nhiễu nội dung.
- Cắt ảnh có chủ đích, dùng `object-fit: cover`; giữ chủ thể chính không bị che bởi tiêu đề hoặc điều hướng.
- Logo, con dấu hoặc họa tiết phương Đông chỉ dùng như chi tiết trang trí có khoảng thở.

## 5. Bố cục và component

- Tạo bố cục điện ảnh với một điểm tập trung chính, vùng âm rộng và lớp nền có chiều sâu.
- Có thể dùng các section toàn màn hình, ảnh tràn khung, carousel và lớp chữ đặt trên ảnh.
- Dùng đường viền mảnh màu đồng/ngà; góc vuông hoặc vát nhẹ phù hợp hơn bo tròn lớn.
- CTA chính có thể dùng nền vàng giấy, chữ đen và hình dáng hơi bất quy tắc như giấy cắt.
- CTA phụ dùng nền trong suốt, chữ ngà, viền mảnh.
- Card nội dung giữ nền rất tối; chỉ nâng sáng khi hover hoặc đang được chọn.
- Điều hướng nhỏ, rõ và cố định khi cần; trạng thái active dùng vàng sáng thay vì màu xanh mặc định.
- Giữ chữ trên ảnh ngắn. Đoạn dài nên đặt trên vùng nền tối riêng để dễ đọc.

## 6. Chuyển động

- Chuyển cảnh chậm, có chủ đích và mượt; ưu tiên fade, dịch chuyển nhẹ, parallax vừa phải.
- Carousel có thể tự chuyển chậm, nhưng cần có nút điều khiển thủ công và trạng thái hiện tại.
- Tránh bounce, zoom liên tục, nhấp nháy, hiệu ứng nảy hoặc chuyển động quá nhanh.
- Tôn trọng `prefers-reduced-motion`; nội dung vẫn phải sử dụng được khi tắt animation.

## 7. Khả năng đọc và responsive

- Duy trì tương phản đủ cao giữa chữ và ảnh/nền.
- Không đặt chữ nhỏ trên vùng ảnh nhiều chi tiết nếu chưa có lớp phủ tối.
- Trên mobile, giảm số lớp trang trí, giữ CTA dễ chạm và tránh để điều hướng che tiêu đề.
- Không ép chữ tiêu đề tràn khỏi màn hình; dùng cỡ chữ linh hoạt theo viewport.
- Các ảnh thumbnail phải giữ tỷ lệ và chiều cao có giới hạn; không để tràn lên footer hoặc nội dung khác.

## 8. Tránh các lựa chọn sau

- Nền trắng sáng, gradient neon, xanh tím công nghệ hoặc màu bão hòa phủ diện rộng.
- Card trắng kiểu dashboard, shadow dày, bo tròn lớn đồng loạt.
- Quá nhiều gold; vàng chỉ có tác dụng khi được dùng tiết chế.
- Dùng texture mạnh phía sau đoạn văn dài.
- Dùng quá nhiều icon emoji, hiệu ứng chuyển động lặp liên tục hoặc chữ trang trí khó đọc.
- Sao chép bố cục một cách máy móc nếu nội dung mới cần cấu trúc khác; giữ tinh thần và hệ thống màu là quan trọng nhất.

## 9. Prompt mẫu để giao cho AI

> Hãy thiết kế thêm **[tên trang/section/component]** cho một website game phiêu lưu hành động mang cảm hứng thần thoại phương Đông. Tuân theo `DESIGN-VIBE-GUIDE.md`: nền đen mực `#080807`, chữ giấy ngà `#E8DFCC`, vàng đồng `#C9A66E`, vàng giấy `#E7C98E`, đỏ gạch `#8E3028` chỉ làm điểm nhấn nhỏ. Phong cách điện ảnh, cổ phong, huyền bí, trầm mặc và cao cấp; dùng serif cho tiêu đề, sans-serif cho nội dung và nhãn. Tạo chiều sâu bằng ảnh tối, gradient và texture thật nhẹ; bố cục có khoảng thở, CTA rõ, tương phản dễ đọc. Thiết kế responsive cho desktop và mobile, hỗ trợ bàn phím và reduced motion. Giữ nhất quán với website hiện có nhưng tổ chức nội dung phù hợp với mục đích của section mới. Tránh giao diện dashboard, màu neon, bo góc lớn và hiệu ứng phô trương.

## 10. Token CSS tham khảo

```css
:root {
  --ink: #080807;
  --charcoal: #10100e;
  --paper: #e8dfcc;
  --bronze: #c9a66e;
  --gold-bright: #e7c98e;
  --rust: #8e3028;
  --smoke: #9a9994;
  --rule: rgba(232, 223, 204, 0.22);
  --display: "Crimson Pro", Georgia, serif;
  --body: Arial, Helvetica, sans-serif;
}
```

Các token hiện được dùng trong `styles.css`; nếu bổ sung màu hoặc font mới, nên cập nhật theo cùng hệ thống thay vì tạo biến rời rạc.
