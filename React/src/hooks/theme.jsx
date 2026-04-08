import { createContext, useContext } from 'react';

export const ThemeContext = createContext({
  dark: false,
  toggleTheme: () => {},
});

export const useThemeMode = () => useContext(ThemeContext);
