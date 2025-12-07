// ===================================
// LAYOUT SCRIPTS
// Wishlist, AJAX Setup, Chatbot
// ===================================

(function ($) {
    'use strict';

    // =============================
    // AJAX SETUP WITH ANTI-FORGERY TOKEN
    // =============================
    function setupAjaxWithToken() {
        var token = $('input[name="__RequestVerificationToken"]').val();
        console.log('Anti-forgery token:', token);
        if (token) {
            $.ajaxSetup({
                beforeSend: function (xhr) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }
            });
        }
    }

    // =============================
    // WISHLIST SIDEBAR FUNCTIONS
    // =============================
    function reloadWishlistSidebar() {
        console.log('Reloading wishlist sidebar...');
        $.ajax({
            url: '/Module/LoadWishlistPartial',
            type: 'GET',
            success: function (html) {
                console.log('Wishlist sidebar reloaded successfully');
                $('.like-offcanvas-body').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error reloading wishlist:', error);
            }
        });
    }

    function openWishlistSidebar() {
        reloadWishlistSidebar();
        $('.like-offcanvas').addClass('active');
        $('.like-offcanvas-overlay').addClass('active');
        $('.like-float-btn').addClass('active');
    }

    function closeWishlistSidebar() {
        $('.like-offcanvas').removeClass('active');
        $('.like-offcanvas-overlay').removeClass('active');
        $('.like-float-btn').removeClass('active');
    }

    function setupWishlistEvents() {
        // Open sidebar
        $('.like-float-btn').on('click', function (e) {
            e.stopPropagation();
            console.log('Like button clicked');
            openWishlistSidebar();
        });

        // Close button
        $('.like-close-btn').on('click', function (e) {
            e.stopPropagation();
            console.log('Close button clicked');
            closeWishlistSidebar();
        });

        // Overlay click
        $('.like-offcanvas-overlay').on('click', function () {
            console.log('Overlay clicked');
            closeWishlistSidebar();
        });

        // Prevent sidebar click from closing
        $('.like-offcanvas').on('click', function (e) {
            e.stopPropagation();
        });
    }

    // =============================
    // REMOVE FAVORITE ITEM
    // =============================
    window.removeFavoriteItem = function (maSach, btnElement) {
        console.log('=== removeFavoriteItem CALLED ===');
        console.log('MaSach:', maSach);

        if (!confirm('Bạn có chắc muốn bỏ yêu thích sản phẩm này?')) {
            return;
        }

        var $btn = $(btnElement);
        var $item = $btn.closest('.like-list-item');

        $.ajax({
            url: '/YeuThich/RemoveFavorite',
            type: 'POST',
            data: { maSach: maSach },
            success: function (res) {
                console.log('AJAX Response:', res);
                if (res.success) {
                    $item.fadeOut(300, function () {
                        $(this).remove();

                        if ($('.like-list-item').length === 0) {
                            $('.like-list').html('<p class="text-center like-empty-message" style="padding: 20px 0;">Bạn chưa có sản phẩm yêu thích nào.</p>');
                        }
                    });
                    if (typeof showCustomToast === 'function') {
                        showCustomToast(res.message || 'Đã xóa khỏi yêu thích');
                    }
                } else {
                    alert(res.message || 'Có lỗi xảy ra');
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', xhr.responseText);
                alert('Có lỗi kết nối. Vui lòng thử lại!');
            }
        });
    };

    // =============================
    // CHATBOT FUNCTIONS
    // =============================
    var chatHistory = [];

    function toggleChat() {
        var chatBox = document.getElementById("chatbot-container");
        if (chatBox.style.display === "none" || chatBox.style.display === "") {
            chatBox.style.display = "flex";
            document.getElementById("chat-input").focus();
        } else {
            chatBox.style.display = "none";
        }
    }

    function appendMessage(text, className) {
        var chatBox = document.getElementById("chat-box");
        var msgDiv = document.createElement("div");
        msgDiv.className = "message " + className;
        msgDiv.innerHTML = text.replace(/\n/g, "<br>");
        chatBox.appendChild(msgDiv);
        scrollToBottom();
    }

    function scrollToBottom() {
        var chatBox = document.getElementById("chat-box");
        chatBox.scrollTop = chatBox.scrollHeight;
    }

    function sendMessage() {
        var inputField = document.getElementById("chat-input");
        var userText = inputField.value.trim();
        if (userText === "") return;

        appendMessage(userText, "user-message");
        inputField.value = "";

        var typingIndicator = document.getElementById("typing-indicator");
        typingIndicator.style.display = "block";
        scrollToBottom();

        $.ajax({
            url: '/Chatbot/SendMessage',
            type: 'POST',
            data: {
                userMessage: userText,
                historyJson: JSON.stringify(chatHistory)
            },
            success: function (response) {
                typingIndicator.style.display = "none";
                if (response.success) {
                    appendMessage(response.reply, "bot-message");
                    if (response.history) {
                        chatHistory = JSON.parse(response.history);
                    }
                } else {
                    appendMessage("Lỗi hệ thống: " + response.reply, "bot-message");
                }
            },
            error: function (err) {
                typingIndicator.style.display = "none";
                appendMessage("⚠️ Không thể kết nối tới máy chủ. Vui lòng thử lại!", "bot-message");
                console.log("Chatbot Error:", err);
            }
        });
    }

    function setupChatbotEvents() {
        // Toggler button
        $('#chatbot-toggler').on('click', function () {
            toggleChat();
        });

        // Enter key
        $('#chat-input').on('keypress', function (event) {
            if (event.key === "Enter") {
                sendMessage();
            }
        });

        // Send button (if needed, add onclick in HTML)
        window.sendMessage = sendMessage;
        window.toggleChat = toggleChat;
    }

    // =============================
    // DOCUMENT READY
    // =============================
    $(document).ready(function () {
        console.log('Layout scripts initialized');
        
        setupAjaxWithToken();
        setupWishlistEvents();
        setupChatbotEvents();

        // Expose reload function globally
        window.reloadWishlistSidebar = reloadWishlistSidebar;
    });

})(jQuery);
