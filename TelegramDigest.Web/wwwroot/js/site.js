// Enable tooltips everywhere
document.addEventListener("DOMContentLoaded", function () {
  var tooltipTriggerList = [].slice.call(
    document.querySelectorAll('[data-bs-toggle="tooltip"]'),
  );
  var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });

  // Auto-hide alerts after 5 seconds
  setTimeout(function () {
    $(".alert").alert("close");
  }, 5000);
});

// Confirm delete actions
function confirmDelete(message) {
  return confirm(message || "Are you sure you want to delete this item?");
}
