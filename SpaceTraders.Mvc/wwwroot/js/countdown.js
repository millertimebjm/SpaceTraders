// countdown.js

function startCountdowns() {
  const countdownElements = document.querySelectorAll('.countdown');

  countdownElements.forEach((el) => {
    const endTime = new Date(el.getAttribute('data-countdown'));

    function updateCountdown() {
      const now = new Date();
      let diff = Math.floor((endTime - now) / 1000); // seconds

      if (diff <= 0) {
        el.textContent = 'Done';
        return;
      }

      const hours = Math.floor(diff / 3600);
      diff %= 3600;
      const minutes = Math.floor(diff / 60);
      const seconds = diff % 60;

      if (hours > 0) {
        el.textContent = `${hours}h`;
      } else if (minutes > 0) {
        el.textContent = `${minutes}m`;
      } else {
        el.textContent = `${seconds}s`;
      }

      // Continue updating every second
      setTimeout(updateCountdown, 1000);
    }

    updateCountdown();
  });
}

// Start countdowns on DOM load
document.addEventListener('DOMContentLoaded', startCountdowns);
