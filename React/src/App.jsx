import { useMemo, useState, useEffect } from 'react';
import { ConfigProvider, App as AntApp, theme as antdTheme } from 'antd';
import ptBR from 'antd/es/locale/pt_BR';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider, ThemeContext } from '@hooks';
import { ErrorBoundary } from '@components/UI';
import { GlobalStyle, colors } from '@styles';
import router from './routes';

const THEME_KEY = 'framework:darkMode';

function App() {
  const [dark, setDark] = useState(() => localStorage.getItem(THEME_KEY) === '1');

  useEffect(() => {
    localStorage.setItem(THEME_KEY, dark ? '1' : '0');
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
  }, [dark]);

  const themeConfig = useMemo(
    () => ({
      algorithm: dark ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
      token: {
        colorPrimary: colors.primary,
        colorPrimaryBg: dark ? undefined : colors.background,
        controlHeight: 48,
        motion: false,
        motionDurationFast: 0,
        motionDurationMid: 0,
        motionDurationSlow: 0,
      },
      hashed: false,
      cssVar: false,
    }),
    [dark]
  );

  const themeCtx = useMemo(
    () => ({
      dark,
      toggleTheme: () => setDark((d) => !d),
    }),
    [dark]
  );

  return (
    <ConfigProvider locale={ptBR} theme={themeConfig}>
      <AntApp>
        <GlobalStyle />
        <ErrorBoundary>
          <ThemeContext.Provider value={themeCtx}>
            <AuthProvider>
              <RouterProvider router={router} />
            </AuthProvider>
          </ThemeContext.Provider>
        </ErrorBoundary>
      </AntApp>
    </ConfigProvider>
  );
}

export default App;
