(() => {
  const root = document.documentElement;
  const key = 'oakideas-site-theme';
  const button = document.querySelector('[data-theme-toggle]');
  const currentYear = document.querySelector('[data-current-year]');

  const setTheme = (theme) => {
    root.dataset.theme = theme;
    localStorage.setItem(key, theme);
  };

  const preferred = localStorage.getItem(key)
    || (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
  setTheme(preferred);

  button?.addEventListener('click', () => {
    setTheme(root.dataset.theme === 'light' ? 'dark' : 'light');
  });

  if (currentYear) {
    currentYear.textContent = new Date().getFullYear().toString();
  }
})();
