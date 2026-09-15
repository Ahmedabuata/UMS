import React, { createContext, useContext, useState, useEffect } from 'react';

const themesList = [
  { id: 'blue', name: 'Blue', icon: '💙' },
  { id: 'dark', name: 'Dark', icon: '🌙' },
  { id: 'green', name: 'Green', icon: '💚' },
  { id: 'light', name: 'Light', icon: '☀️' },
  { id: 'olive', name: 'Olive', icon: '🫒' },
  { id: 'orange', name: 'Orange', icon: '🧡' },
  { id: 'purple', name: 'Purple', icon: '💜' },
];

const ThemeContext = createContext();

export const ThemeProvider = ({ children }) => {
  const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'blue');

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const changeTheme = (newTheme) => setTheme(newTheme);

  return (
    <ThemeContext.Provider value={{ theme, changeTheme, setTheme: changeTheme, availableThemes: themesList, themes: themesList }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be inside ThemeProvider');
  return ctx;
};

export default ThemeContext;
