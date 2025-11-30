/**
 * Checkout Page JavaScript
 * Handles Vietnamese address API integration and checkout functionality
 */

(function ($) {
    'use strict';

    // Constants
    const API_URL = 'https://provinces.open-api.vn/api/';
    const SHIPPING_FEES = {
        standard: 30000,
        express: 50000
    };

    // Global variables
    let provinces = [];
    let districts = [];
    let wards = [];
    let currentShippingFee = SHIPPING_FEES.standard;
    let currentDiscount = 0;

    // Initialize
    $(document).ready(function () {
        initializeAddressAPI();
        initializeEventHandlers();
        updateTotalPrice();
    });

    /**
     * Initialize Vietnamese Address API
     */
    function initializeAddressAPI() {
        // Load provinces
        $.ajax({
            url: API_URL + 'p/',
            method: 'GET',
            dataType: 'json',
            success: function (data) {
                provinces = data;
                populateProvinces();
            },
            error: function (xhr, status, error) {
                console.error('Error loading provinces:', error);
                showNotification('Không thể tải danh sách tỉnh/thành phố', 'error');
            }
        });
    }

    /**
     * Populate provinces dropdown
     */
    function populateProvinces() {
        const $select = $('#tinhThanhPho');
        $select.empty().append('<option value="">Chọn Tỉnh/TP</option>');

        provinces.forEach(function (province) {
            $select.append(
                $('<option></option>')
                    .val(province.code)
                    .text(province.name)
                    .data('name', province.name)
            );
        });
    }

    /**
     * Load districts based on selected province
     */
    function loadDistricts(provinceCode) {
        $.ajax({
            url: API_URL + 'p/' + provinceCode + '?depth=2',
            method: 'GET',
            dataType: 'json',
            success: function (data) {
                districts = data.districts || [];
                populateDistricts();
                $('#quanHuyen').prop('disabled', false);
            },
            error: function (xhr, status, error) {
                console.error('Error loading districts:', error);
                showNotification('Không thể tải danh sách quận/huyện', 'error');
            }
        });
    }

    /**
     * Populate districts dropdown
     */
    function populateDistricts() {
        const $select = $('#quanHuyen');
        $select.empty().append('<option value="">Chọn Quận/Huyện</option>');

        districts.forEach(function (district) {
            $select.append(
                $('<option></option>')
                    .val(district.code)
                    .text(district.name)
                    .data('name', district.name)
            );
        });

        // Reset wards
        $('#phuongXa').empty().append('<option value="">Chọn Phường/Xã</option>').prop('disabled', true);
    }

    /**
     * Load wards based on selected district
     */
    function loadWards(districtCode) {
        $.ajax({
            url: API_URL + 'd/' + districtCode + '?depth=2',
            method: 'GET',
            dataType: 'json',
            success: function (data) {
                wards = data.wards || [];
                populateWards();
                $('#phuongXa').prop('disabled', false);
            },
            error: function (xhr, status, error) {
                console.error('Error loading wards:', error);
                showNotification('Không thể tải danh sách phường/xã', 'error');
            }
        });
    }

    /**
     * Populate wards dropdown
     */
    function populateWards() {
        const $select = $('#phuongXa');
        $select.empty().append('<option value="">Chọn Phường/Xã</option>');

        wards.forEach(function (ward) {
            $select.append(
                $('<option></option>')
                    .val(ward.code)
                    .text(ward.name)
                    .data('name', ward.name)
            );
        });
    }

    /**
     * Calculate shipping fee based on location
     */
    function calculateShippingFee() {
        const provinceCode = $('#tinhThanhPho').val();
        const shippingMethod = $('input[name="PhuongThucGiaoHang"]:checked').val();

        // Get base fee based on shipping method
        let baseFee = shippingMethod === '2' ? SHIPPING_FEES.express : SHIPPING_FEES.standard;

        // Add extra fee for remote provinces (example logic)
        // You can customize this based on specific province codes
        const remoteProvivinces = []; // Add remote province codes here
        if (remoteProvivinces.includes(provinceCode)) {
            baseFee += 20000;
        }

        currentShippingFee = baseFee;
        updateShippingFeeDisplay();
        updateTotalPrice();
    }

    /**
     * Update shipping fee display
     */
    function updateShippingFeeDisplay() {
        const formattedFee = formatCurrency(currentShippingFee);
        $('#shippingFee').text(formattedFee);
        $('#phiVanChuyenText').text(formattedFee);
        $('#hiddenShippingFee').val(currentShippingFee);
    }

    /**
     * Apply discount code
     * NOTE: Only ONE discount code can be applied at a time.
     * Discount types:
     * - percent: Discount based on percentage of order total
     * - fixed: Fixed amount discount on order total
     * - freeship: Free shipping (no shipping fee)
     */
    function applyDiscountCode() {
        const code = $('#discountCode').val().trim();

        if (!code) {
            showDiscountMessage('Vui lòng nhập mã giảm giá', 'error');
            return;
        }

        // KIỂM TRA NGHIÊM NGẶT: Nếu đã có mã giảm giá được áp dụng
        const currentCode = $('#hiddenDiscountCode').val();
        if (currentCode) {
            if (currentCode === code) {
                // Cùng mã - thông báo đã áp dụng rồi
                showDiscountMessage('Mã giảm giá này đã được áp dụng!', 'error');
                return;
            } else {
                // Mã khác - bắt buộc confirm
                if (!confirm('⚠️ CHÚ Ý: Chỉ được áp dụng 1 mã giảm giá!\n\n' +
                    'Bạn đã áp dụng mã "' + currentCode + '".\n' +
                    'Bạn có muốn HỦY mã cũ và áp dụng mã "' + code + '" không?')) {
                    // User không đồng ý - giữ nguyên mã cũ
                    $('#discountCode').val(''); // Clear input
                    return;
                }
                // User đồng ý - reset mã giảm giá cũ
                resetDiscount();
            }
        }

        // Show loading state
        $('#applyDiscountBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Đang xử lý...');

        // Gọi API để validate mã giảm giá
        $.ajax({
            url: '/GioHang/ValidateDiscountCode',
            method: 'POST',
            data: { code: code },
            success: function (response) {
                if (response.success) {
                    applyDiscount(response.discount, code);
                    showDiscountMessage('✓ Áp dụng mã giảm giá thành công!', 'success');
                    $('#discountCode').val(''); // Clear input sau khi áp dụng thành công
                } else {
                    showDiscountMessage(response.message || 'Mã giảm giá không hợp lệ hoặc đã hết hạn', 'error');
                }
                $('#applyDiscountBtn').prop('disabled', false).html('<i class="fas fa-tag me-1"></i>Áp dụng');
            },
            error: function () {
                showDiscountMessage('Lỗi kết nối. Vui lòng thử lại sau', 'error');
                $('#applyDiscountBtn').prop('disabled', false).html('<i class="fas fa-tag me-1"></i>Áp dụng');
            }
        });
    }

    /**
     * Reset discount
     * Completely removes all discount information and restores default shipping fee
     */
    function resetDiscount() {
        // Lưu thông tin mã cũ để log (nếu cần)
        const oldCode = $('#hiddenDiscountCode').val();

        // 1. Reset discount amount
        currentDiscount = 0;

        // 2. Khôi phục lại phí vận chuyển gốc (nếu đang bị freeship)
        const selectedShippingMethod = $('input[name="PhuongThucGiaoHang"]:checked').val();
        currentShippingFee = selectedShippingMethod === '2' ? SHIPPING_FEES.express : SHIPPING_FEES.standard;

        // 3. Clear ALL hidden fields
        $('#hiddenDiscountCode').val('');
        $('#hiddenDiscountAmount').val('0');

        // 4. Hide and clear discount row
        $('#discountRow').hide();
        $('#discountAmount').text('-0đ');

        // 5. Clear discount code input
        $('#discountCode').val('');

        // 6. Remove applied discount info box
        $('#appliedDiscountInfo').remove();

        // 7. Clear any discount messages
        $('#discountMessage').html('').hide();

        // 8. Update display
        updateShippingFeeDisplay();
        updateTotalPrice();

        // Log for debugging
        if (oldCode) {
            console.log('Discount reset - Removed code:', oldCode);
        }
    }

    /**
     * Apply discount to order
     * This function handles three types of discounts:
     * 1. percent: Percentage discount on order subtotal
     * 2. fixed: Fixed amount discount on order subtotal
     * 3. freeship: Free shipping (sets shipping fee to 0)
     * 
     * IMPORTANT: Only ONE discount code can be active at a time
     */
    function applyDiscount(discount, code) {
        const subtotal = getSubtotal();

        // Reset hoàn toàn trước khi áp dụng mã mới
        currentDiscount = 0;

        // Khôi phục phí ship về mặc định
        const selectedShippingMethod = $('input[name="PhuongThucGiaoHang"]:checked').val();
        currentShippingFee = selectedShippingMethod === '2' ? SHIPPING_FEES.express : SHIPPING_FEES.standard;

        // Xóa UI cũ
        $('#appliedDiscountInfo').remove();
        $('#discountRow').hide();

        // Áp dụng mã mới dựa theo loại mã giảm giá
        if (discount.type === 'percent') {
            // Giảm theo phần trăm trên tổng tiền hàng
            currentDiscount = Math.round(subtotal * discount.value / 100);
        } else if (discount.type === 'fixed') {
            // Giảm số tiền cố định (không vượt quá tổng tiền hàng)
            currentDiscount = Math.min(discount.value, subtotal);
        } else if (discount.type === 'freeship') {
            // ✅ Mã freeship: CHỈ miễn phí vận chuyển, KHÔNG giảm giá thêm
            // QUAN TRỌNG: currentDiscount = 0 để tránh bị trừ 2 lần
            currentDiscount = 0; // KHÔNG có giảm giá
            currentShippingFee = 0; // Set phí ship = 0
            updateShippingFeeDisplay(); // Cập nhật hiển thị phí ship
        }

        // Update hidden fields
        $('#hiddenDiscountCode').val(code);
        $('#hiddenDiscountAmount').val(currentDiscount);

        // Update display
        if (currentDiscount > 0) {
            // Chỉ hiển thị dòng "Giảm giá" khi có giảm giá thực sự
            $('#discountRow').show();
            $('#discountAmount').text('-' + formatCurrency(currentDiscount));
        }

        // Luôn hiển thị thông tin mã đã áp dụng (cho tất cả loại mã)
        const discountTypeText = discount.type === 'freeship' ? 'Miễn phí vận chuyển' :
            discount.type === 'percent' ? discount.value + '% giảm giá' :
                formatCurrency(discount.value) + ' giảm giá';

        $('#discountMessage').after(
            '<div id="appliedDiscountInfo" class="mt-2 p-2 border border-success rounded bg-light">' +
            '<div class="d-flex justify-content-between align-items-center">' +
            '<div>' +
            '<span class="text-success"><i class="fas fa-check-circle me-2"></i>Đã áp dụng mã: <strong>' + code + '</strong></span>' +
            '<br><small class="text-muted">' + discountTypeText + '</small>' +
            '</div>' +
            '<button type="button" id="btnRemoveDiscount" class="btn btn-sm btn-outline-danger">' +
            '<i class="fas fa-times me-1"></i>Hủy' +
            '</button>' +
            '</div>' +
            '</div>'
        );
        updateTotalPrice();
    }

    /**
     * Get subtotal from cart items
     */
    function getSubtotal() {
        const subtotalText = $('#subtotal').text().replace(/[^\d]/g, '');
        return parseInt(subtotalText) || 0;
    }

    /**
     * Update total price
     */
    function updateTotalPrice() {
        const subtotal = getSubtotal();
        const total = subtotal + currentShippingFee - currentDiscount;

        $('#totalAmount').text(formatCurrency(total));
        $('#hiddenTotalAmount').val(total);
    }

    /**
     * Format number as Vietnamese currency
     */
    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount).replace('₫', 'đ');
    }

    /**
     * Show discount message
     */
    function showDiscountMessage(message, type) {
        const $msg = $('#discountMessage');
        $msg.removeClass('success error')
            .addClass(type)
            .text(message)
            .fadeIn();

        setTimeout(function () {
            $msg.fadeOut();
        }, 5000);
    }

    /**
     * Show notification
     */
    function showNotification(message, type) {
        // You can implement a toast/notification system here
        console.log(type + ': ' + message);
    }

    /**
     * Close modal completely and remove backdrop
     * Fix for modal overlay staying after close
     */
    function closeModalCompletely() {
        const $modal = $('#discountModal');

        // Try Bootstrap 5 API first
        const modalEl = document.getElementById('discountModal');
        const bsModal = bootstrap.Modal.getInstance(modalEl);
        if (bsModal) {
            bsModal.hide();
        } else {
            // Fallback to jQuery
            $modal.modal('hide');
        }

        // Force remove backdrop after a delay
        setTimeout(function () {
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css('overflow', '');
            $('body').css('padding-right', '');
        }, 300);
    }

    /**
     * Load available discount codes and show in modal
     */
    function loadAvailableDiscounts() {
        // Show loading in modal
        $('#discountModal .modal-body').html('<div class="text-center p-3"><i class="fas fa-spinner fa-spin fa-2x"></i><p class="mt-2">Đang tải mã giảm giá...</p></div>');

        // Show modal
        $('#discountModal').modal('show');

        $.ajax({
            url: '/GioHang/GetAvailableDiscounts',
            method: 'GET',
            success: function (response) {
                if (response.success && response.data && response.data.length > 0) {
                    let html = '<div class="list-group">';

                    response.data.forEach(function (discount) {
                        let description = '';
                        let icon = '';

                        if (discount.LoaiMa === 'percent') {
                            description = 'Giảm ' + discount.PhanTramGiam + '% trên đơn hàng';
                            icon = '<i class="fas fa-percent text-primary"></i>';
                        } else if (discount.LoaiMa === 'fixed') {
                            description = 'Giảm ' + discount.SoTienGiam.toLocaleString('vi-VN') + 'đ';
                            icon = '<i class="fas fa-money-bill-wave text-success"></i>';
                        } else {
                            description = 'Miễn phí vận chuyển';
                            icon = '<i class="fas fa-shipping-fast text-info"></i>';
                        }

                        // Kiểm tra nếu mã đang được áp dụng
                        const currentCode = $('#hiddenDiscountCode').val();
                        const isApplied = currentCode === discount.MaCode;

                        html += '<div class="list-group-item">';
                        html += '<div class="d-flex w-100 justify-content-between align-items-center">';
                        html += '<div class="flex-grow-1">';
                        html += '<h6 class="mb-1">' + icon + ' ' + discount.MaCode + '</h6>';
                        html += '<p class="mb-1">' + discount.TenChuongTrinh + '</p>';
                        html += '<small class="text-muted">' + description + '</small><br>';
                        html += '<small class="text-muted">HSD: ' + discount.NgayKetThuc + ' | Còn ' + discount.SoLuongConLai + ' lượt</small>';
                        html += '</div>';

                        if (isApplied) {
                            html += '<span class="badge bg-success ms-2">Đang áp dụng</span>';
                        } else {
                            html += '<button type="button" class="btn btn-sm btn-outline-primary btn-apply-discount ms-2" data-code="' + discount.MaCode + '">';
                            html += '<i class="fas fa-tag me-1"></i>Áp dụng';
                            html += '</button>';
                        }

                        html += '</div>';
                        html += '</div>';
                    });

                    html += '</div>';
                    html += '<div class="alert alert-info mt-3 mb-0">';
                    html += '<i class="fas fa-info-circle me-2"></i>';
                    html += '<strong>Lưu ý:</strong> Chỉ được áp dụng 1 mã giảm giá cho mỗi đơn hàng.';
                    html += '</div>';

                    $('#discountModal .modal-body').html(html);
                } else {
                    $('#discountModal .modal-body').html(
                        '<div class="text-center p-3">' +
                        '<i class="fas fa-tag fa-3x text-muted mb-3"></i>' +
                        '<p class="text-muted">Hiện không có mã giảm giá nào khả dụng.</p>' +
                        '</div>'
                    );
                }
            },
            error: function () {
                $('#discountModal .modal-body').html(
                    '<div class="alert alert-danger m-0">' +
                    '<i class="fas fa-exclamation-triangle me-2"></i>' +
                    'Không thể tải danh sách mã giảm giá. Vui lòng thử lại sau.' +
                    '</div>'
                );
            }
        });
    }

    /**
     * Validate form before submission
     */
    function validateForm() {
        let isValid = true;
        const requiredFields = [
            'HoTen',
            'SoDienThoai',
            'Email',
            'TinhThanhPho',
            'QuanHuyen',
            'PhuongXa',
            'DiaChiChiTiet'
        ];

        requiredFields.forEach(function (fieldName) {
            const $field = $('[name="' + fieldName + '"]');
            if (!$field.val() || $field.val().trim() === '') {
                $field.addClass('is-invalid');
                isValid = false;
            } else {
                $field.removeClass('is-invalid');
            }
        });

        // Email validation
        const email = $('[name="Email"]').val();
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (email && !emailRegex.test(email)) {
            $('[name="Email"]').addClass('is-invalid');
            isValid = false;
        }

        // Phone validation
        const phone = $('[name="SoDienThoai"]').val();
        const phoneRegex = /^[0-9]{10,11}$/;
        if (phone && !phoneRegex.test(phone.replace(/\s/g, ''))) {
            $('[name="SoDienThoai"]').addClass('is-invalid');
            isValid = false;
        }

        return isValid;
    }

    /**
     * Initialize event handlers
     */
    function initializeEventHandlers() {
        // Province change
        $('#tinhThanhPho').on('change', function () {
            const provinceCode = $(this).val();
            if (provinceCode) {
                loadDistricts(provinceCode);
                calculateShippingFee();
            } else {
                $('#quanHuyen').empty().append('<option value="">Chọn Quận/Huyện</option>').prop('disabled', true);
                $('#phuongXa').empty().append('<option value="">Chọn Phường/Xã</option>').prop('disabled', true);
            }
        });

        // District change
        $('#quanHuyen').on('change', function () {
            const districtCode = $(this).val();
            if (districtCode) {
                loadWards(districtCode);
            } else {
                $('#phuongXa').empty().append('<option value="">Chọn Phường/Xã</option>').prop('disabled', true);
            }
        });

        // Shipping method change
        $('input[name="PhuongThucGiaoHang"]').on('change', function () {
            const discountCode = $('#hiddenDiscountCode').val();

            // Nếu đã có mã giảm giá được áp dụng
            if (discountCode) {
                if (confirm('Thay đổi phương thức vận chuyển sẽ hủy mã giảm giá hiện tại. Bạn có muốn tiếp tục?')) {
                    // User chọn OK: Reset mã giảm giá và cập nhật phí ship
                    resetDiscount();
                    calculateShippingFee();
                } else {
                    // User chọn Cancel: Giữ nguyên lựa chọn cũ
                    const oldMethod = currentShippingFee === SHIPPING_FEES.express ? '2' : '1';
                    $('input[name="PhuongThucGiaoHang"][value="' + oldMethod + '"]').prop('checked', true);
                }
            } else {
                // Chưa có mã giảm giá: Cập nhật phí ship bình thường
                calculateShippingFee();
            }
        });

        // Apply discount code
        $('#applyDiscountBtn').on('click', function () {
            applyDiscountCode();
        });

        // Remove discount code
        $(document).on('click', '#btnRemoveDiscount', function () {
            if (confirm('Bạn có chắc muốn hủy mã giảm giá đã áp dụng?')) {
                resetDiscount();
                showDiscountMessage('Đã hủy mã giảm giá', 'success');
                setTimeout(function () {
                    $('#discountMessage').fadeOut();
                }, 2000);
            }
        });

        // Enter key on discount code input
        $('#discountCode').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                applyDiscountCode();
            }
        });

        // Show discount popup
        $('#btnShowDiscountPopup').on('click', function () {
            loadAvailableDiscounts();
        });

        // Apply discount from modal - supports both .btn-apply-discount and .btn-apply-code
        $(document).on('click', '.btn-apply-discount, .btn-apply-code', function () {
            const code = $(this).data('code');

            // KIỂM TRA NGHIÊM NGẶT: Nếu đã có mã giảm giá được áp dụng
            const currentCode = $('#hiddenDiscountCode').val();
            if (currentCode) {
                if (currentCode === code) {
                    // Cùng mã - đóng modal và thông báo
                    closeModalCompletely();
                    showDiscountMessage('Mã giảm giá này đã được áp dụng!', 'error');
                    return;
                } else {
                    // Mã khác - bắt buộc confirm
                    if (!confirm('⚠️ CHÚ Ý: Chỉ được áp dụng 1 mã giảm giá!\n\n' +
                        'Bạn đã áp dụng mã "' + currentCode + '".\n' +
                        'Bạn có muốn HỦY mã cũ và áp dụng mã "' + code + '" không?')) {
                        return; // User không đồng ý
                    }
                    // User đồng ý - reset mã giảm giá cũ
                    resetDiscount();
                }
            }

            // ✅ Đặt code vào input field
            $('#discountCode').val(code);

            // ✅ Đóng modal hoàn toàn
            closeModalCompletely();

            // ✅ Đợi một chút rồi áp dụng mã
            setTimeout(function () {
                applyDiscountCode();
            }, 300);
        });

        // Form submission
        $('#checkoutForm').on('submit', function (e) {
            if (!validateForm()) {
                e.preventDefault();
                showNotification('Vui lòng điền đầy đủ thông tin bắt buộc', 'error');

                // Scroll to first invalid field
                const $firstInvalid = $('.is-invalid').first();
                if ($firstInvalid.length) {
                    $('html, body').animate({
                        scrollTop: $firstInvalid.offset().top - 100
                    }, 500);
                }
                return false;
            }

            // Validate discount code before submission
            const discountCode = $('#hiddenDiscountCode').val();
            const discountAmount = parseFloat($('#hiddenDiscountAmount').val()) || 0;

            if (discountCode && discountAmount <= 0) {
                e.preventDefault();
                alert('Mã giảm giá không hợp lệ. Vui lòng áp dụng lại hoặc xóa mã.');
                resetDiscount();
                return false;
            }

            // Show loading state
            $('.btn-order').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...');
        });

        // Remove validation error on input
        $('input, select, textarea').on('input change', function () {
            $(this).removeClass('is-invalid');
        });
    }

})(jQuery);
