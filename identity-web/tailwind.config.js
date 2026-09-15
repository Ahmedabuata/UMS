export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'bg-primary': 'var(--bg-primary)',
        'bg-secondary': 'var(--bg-secondary)',
        'text-main': 'var(--text-main)',
        'text-muted': 'var(--text-muted)',
        'border-color': 'var(--border-color)',
        'accent': 'var(--accent-color)',
      }
    },
  },
  plugins: [],
}
