document.addEventListener("DOMContentLoaded", function () {

    // Product Detail Modal (Index.cshtml)
    const modalContainer = document.getElementById("productDetailContainer");
    if (modalContainer) {
        document.querySelectorAll(".product-page-card-body").forEach(cardBody => {
            cardBody.addEventListener("click", async function () {
                const rowKey = this.getAttribute("data-rowkey");
                if (!rowKey) return;

                try {
                    // Fetch product detail partial view
                    const response = await fetch(`/Product/Details?rowKey=${rowKey}`);
                    const html = await response.text();

                    // Clear old modal & insert new one
                    modalContainer.innerHTML = html;

                    // Initialize and show modal
                    const modalEl = document.getElementById("productDetailModal");
                    const modal = new bootstrap.Modal(modalEl);

                    modalEl.addEventListener("hidden.bs.modal", () => {
                        modal.dispose();
                        modalContainer.innerHTML = "";
                    });

                    modal.show();
                } catch (err) {
                    console.error("Error loading product modal:", err);
                }
            });
        });

        // Delegated Add-to-Cart click listener
        document.addEventListener("click", async function (event) {
            if (event.target && event.target.id === "addToCartBtn") {
                const rowKey = event.target.getAttribute("data-rowkey");
                if (!rowKey) return;

                try {
                    const response = await fetch(`/Cart/AddToCart/${rowKey}`, { method: "POST" });

                    if (response.ok) {
                        alert("Product added to cart!");
                    } else {
                        alert("Failed to add product to cart.");
                    }
                } catch (error) {
                    console.error("Add to Cart failed:", error);
                    alert("Could not connect to server.");
                }
            }
        });
    }

    // Image Preview (Add/Edit Product)
    const imageInput = document.getElementById('imageInput');
    const imagePreview = document.getElementById('imagePreview');

    if (imageInput && imagePreview) {
        imageInput.addEventListener('change', function (event) {
            const file = event.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    imagePreview.src = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    }

});