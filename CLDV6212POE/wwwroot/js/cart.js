$(function () {

    // Refreshs cart items & count
    function refreshCart() {
        const $container = $("#cart-items-container");
        if (!$container.length) return;

        $.get('/Cart/GetCartItems', function (items) {
            const count = items.length;
            $("#cart-count").text(count);

            let html = '';
            if (count === 0) {
                html = '<p class="text-center text-muted mb-0">Your cart is empty</p>';
            } else {
                let total = 0;

                items.forEach(item => {
                    total += Number(item.productPrice) * Number(item.quantity);
                    const formattedPrice = "R " + Number(item.productPrice).toLocaleString();

                    html += `
                        <div class="cart-item d-flex align-items-start p-2 border-bottom">
                            <img src="${item.imageUrl}" width="50" height="50" class="rounded me-2">
                            <div class="flex-grow-1">
                                <strong>${item.productName}</strong><br>
                                <small>Qty: ${item.quantity}</small>
                            </div>
                            <div class="text-end">
                                <span>${formattedPrice}</span><br>
                                <button class="btn btn-sm btn-danger mt-1 remove-from-cart-btn"
                                        data-product-id="${item.productId}" title="Remove">
                                    <i class="fa fa-trash"></i>
                                </button>
                            </div>
                        </div>`;
                });

                const formattedTotal = "R " + total.toLocaleString();

                html += `
                <div class="cart-total">
                    <strong>Total: ${formattedTotal}</strong>
                    <div class="buttons d-flex flex-row gap-2 mt-2">
                        <form action="/Order/Create" method="post" class="flex-fill">
                            <button type="submit" class="btn-cart w-100">Complete Order</button>
                        </form>
                        <a href="/Cart" class="btn-cart w-100">View Cart</a>
                    </div>
                </div>`;
            }

            $container.html(html);
        });
    }

    // Add Item to Cart
    $(document).on("click", ".add-to-cart-btn", function () {
        const productId = $(this).data("product-id");
        const productName = $(this).data("product-name");
        const imageUrl = $(this).data("image-url");
        const price = $(this).data("price");

        $.post('/Cart/AddToCart', { productId, productName, imageUrl, price })
            .done(function (res) {
                if (res.success) refreshCart();
                else alert(res.message);
            })
            .fail(function (err) {
                console.error("AddToCart failed:", err);
            });
    });

    // Removes item from Cart
    $(document).on("click", ".remove-from-cart-btn", function () {
        const productId = $(this).data("product-id");

        $.post('/Cart/RemoveFromCart', { productId })
            .done(function (res) {
                if (res.success) {
                    refreshCart();
                } else {
                    alert(res.message);
                }
            })
            .fail(function (err) {
                console.error("RemoveFromCart failed:", err);
            });
    });

    // Logout
    $(document).on("submit", "#logoutForm", function (e) {
        e.preventDefault();

        $.post('/Account/Logout')
            .done(function () {
                location.reload(); // refresh session-dependent UI
            })
            .fail(function () {
                alert("Logout failed. Try again.");
            });
    });

    // Initialize cart dropdown
    const cartToggleEl = document.getElementById('cartMenu');
    if (cartToggleEl) bootstrap.Dropdown.getOrCreateInstance(cartToggleEl);

    // Initial cart load
    refreshCart();
});
