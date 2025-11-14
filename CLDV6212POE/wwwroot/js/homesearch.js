document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById("homeSearchInput");
    const searchBtn = document.getElementById("homeSearchBtn");

    function goToProductPage() {
        const query = searchInput.value.trim();
        if (!query) return;

        // Redirect to Product page search
        window.location.href = `/Product?search=${encodeURIComponent(query)}`;
    }

    // Handle Enter key
    searchInput.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            goToProductPage();
        }
    });

    // Handle search button click
    searchBtn.addEventListener("click", function (e) {
        e.preventDefault();
        goToProductPage();
    });
});
