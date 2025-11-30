// book-common.js
// Xử lý thêm vào giỏ hàng cho các trang sách
$(document).ready(function () {
    $('.btnAddCart').click(function (e) {
        e.preventDefault();
        var masach = $(this).data('masach');
        var url = $(this).data('url') || "/GioHang/ThemGiohangAjax";
        var qty = 1;
        if ($('#qty').length) {
            qty = parseInt($('#qty').val()) || 1;
        }
        $.ajax({
            url: url,
            type: "POST",
            data: { iMasach: masach, qty: qty },
            success: function (res) {
                showCustomToast('Đã thêm sách vào giỏ hàng!');
                if (res.tongsoluong !== undefined) {
                    $('.total-count').text(res.tongsoluong);
                    $('.total-count1').text(res.tongsoluong);
                }
            },
            error: function () {
                alert('Có lỗi xảy ra!');
            }
        });
    });

    // Tab hiệu ứng fade-in cho trang chủ
    $('.tab-btn').click(function(){
        var target = $(this).data('target');
        $('.tab-btn').removeClass('active');
        $(this).addClass('active');
        $('.tab-content').removeClass('active').hide();
        $(target).addClass('active').show();
        // Stagger effect: delay mỗi sản phẩm 100ms
        $(target + ' .fade-in').each(function(i){
            $(this).css('animation-delay', (i * 0.1) + 's');
        });
    });
    // Mặc định hiển thị tab đầu tiên nếu có
    if ($('.tab-btn').length) {
        $('.tab-btn').first().click();
    }
    // Stagger effect cho tab đầu tiên
    $('.tab-content.active .fade-in').each(function(i){
        $(this).css('animation-delay', (i * 0.1) + 's');
    });
});

// Hàm showCustomToast nếu chưa có
if (typeof showCustomToast !== 'function') {
    function showCustomToast(msg) {
        alert(msg);
    }
} 