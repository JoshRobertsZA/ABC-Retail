// Logout Form
$(document).on("submit", "#logoutForm", function (e) {
    e.preventDefault();

    $.post('/Account/Logout', function () {
        // Clear session-dependent elements and reload page
        location.reload();
    }).fail(function () {
        alert("Logout failed. Try again.");
    });
});

// Cart Dropdown Hover Behavior
document.addEventListener("DOMContentLoaded", function () {
    const cartButton = document.getElementById("cartMenu");
    const cartDropdown = document.getElementById("cart-dropdown");

    if (cartButton && cartDropdown) {
        cartButton.addEventListener("mouseenter", function () {
            cartDropdown.classList.add("show");
            cartDropdown.style.display = "block";
        });

        cartButton.addEventListener("mouseleave", function () {
            setTimeout(() => {
                if (!cartDropdown.matches(":hover")) {
                    cartDropdown.classList.remove("show");
                    cartDropdown.style.display = "none";
                }
            }, 200);
        });

        cartDropdown.addEventListener("mouseleave", function () {
            cartDropdown.classList.remove("show");
            cartDropdown.style.display = "none";
        });
    }
});

// Cart Button Login Check
document.addEventListener("DOMContentLoaded", function () {
    const cartButton = document.getElementById("cartMenu");

    if (cartButton) {
        cartButton.addEventListener("click", function (e) {
            // Check login status from Razor variable
            const isLoggedIn = document.body.dataset.loggedIn;

            if (isLoggedIn === "false") {
                e.preventDefault();
                // Open the login modal automatically
                const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
                loginModal.show();
            }
        });
    }
});

// Modal Switching Logic
document.addEventListener('DOMContentLoaded', function () {
    // Handle switching between modals
    const modalLinks = document.querySelectorAll('[data-bs-toggle="modal"][data-bs-target]');

    modalLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            const targetModalId = this.getAttribute('data-bs-target');
            const currentModal = bootstrap.Modal.getInstance(this.closest('.modal'));

            if (currentModal) {
                // Wait for the current modal to fully close before opening the next
                this.addEventListener('hidden.bs.modal', function handler() {
                    const nextModal = new bootstrap.Modal(document.querySelector(targetModalId));
                    nextModal.show();
                    this.removeEventListener('hidden.bs.modal', handler);
                });
                currentModal.hide();
                e.preventDefault();
            }
        });
    });
});

// Login Modal Form Submission
document.addEventListener("DOMContentLoaded", function () {
    const loginForm = document.getElementById("loginForm");

    if (loginForm) {
        loginForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const email = document.getElementById("loginEmail").value.trim();
            const password = document.getElementById("loginPassword").value.trim();
            const errorMsg = document.getElementById("loginError");
            const currentUrl = window.location.pathname;

            errorMsg.style.display = "none";
            errorMsg.textContent = "";

            if (!email || !password) {
                errorMsg.textContent = "Please fill in both fields.";
                errorMsg.style.display = "block";
                return;
            }

            try {
                const response = await fetch("/Account/Login", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ email, password, returnUrl: currentUrl })
                });

                const result = await response.json();

                if (result.success) {
                    const modal = bootstrap.Modal.getInstance(document.getElementById("loginModal"));
                    modal.hide();

                    window.location.href = result.redirectUrl;
                } else {
                    errorMsg.textContent = result.message;
                    errorMsg.style.display = "block";
                }
            } catch (error) {
                console.error("Login error:", error);
                errorMsg.textContent = "Something went wrong. Please try again.";
                errorMsg.style.display = "block";
            }
        });
    }
});

// Register Modal Form Submission
document.addEventListener("DOMContentLoaded", function () {
    const registerForm = document.getElementById("registerForm");

    if (registerForm) {
        registerForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const name = document.getElementById("registerFullName").value.trim();
            const email = document.getElementById("registerEmail").value.trim();
            const password = document.getElementById("registerPassword").value.trim();
            const confirmPassword = document.getElementById("registerConfirmPassword").value.trim();

            if (!name || !email || !password || !confirmPassword) {
                alert("Please fill in all fields.");
                return;
            }

            if (password !== confirmPassword) {
                alert("Passwords do not match!");
                return;
            }

            try {
                const response = await fetch("/Account/Register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ name, email, password })
                });

                const result = await response.json();

                if (result.success) {
                    const modal = bootstrap.Modal.getInstance(document.getElementById("registerModal"));
                    modal.hide();

                    location.reload();
                } else {
                    alert(result.message);
                }
            } catch (error) {
                console.error("Registration error:", error);
                alert("An error occurred. Please try again.");
            }
        });
    }
});