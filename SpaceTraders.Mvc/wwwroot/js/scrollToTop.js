// scrollToTop.js

// Show or hide the button based on scroll position
window.onscroll = function () {
  toggleScrollToTopButton();
};

function toggleScrollToTopButton() {
  const btn = document.getElementById("scrollToTopButton");
  if (!btn) return;

  if (document.body.scrollTop > 100 || document.documentElement.scrollTop > 100) {
    btn.style.display = "block";
  } else {
    btn.style.display = "none";
  }
}

// Scroll to the top smoothly when button is clicked
function topFunction() {
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Optional: set initial styling for the button
document.addEventListener("DOMContentLoaded", function () {
  const btn = document.getElementById("scrollToTopButton");
  if (btn) {
    btn.style.display = "none";
    btn.style.position = "fixed";
    btn.style.bottom = "40px";
    btn.style.right = "40px";
    btn.style.zIndex = "999";
    btn.style.backgroundColor = "#007BFF";
    btn.style.color = "white";
    btn.style.border = "none";
    btn.style.padding = "10px 15px";
    btn.style.borderRadius = "5px";
    btn.style.cursor = "pointer";
    btn.style.boxShadow = "0px 2px 6px rgba(0,0,0,0.3)";
    btn.style.fontSize = "16px";
  }
});
