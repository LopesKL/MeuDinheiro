import { createContext, useContext, useState, useEffect } from 'react';
import Api from '@services/api';

const project = 'framework';

const USE_MOCK_AUTH = import.meta.env.VITE_USE_MOCK_AUTH === 'true';

const MOCK_USERS = {
  admin: {
    username: 'admin',
    password: 'admin123',
    user: { id: '1', name: 'Administrador', email: 'admin@example.com', username: 'admin' },
    roles: ['Admin'],
    accessToken: 'mock-token-admin-' + Date.now(),
  },
};

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const storedToken = localStorage.getItem(`${project}:token`);
    const storedUser = localStorage.getItem(`${project}:user`);
    const storedRoles = localStorage.getItem(`${project}:userRoles`);

    if (storedToken && storedUser) {
      try {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));
        if (!storedRoles) localStorage.setItem(`${project}:userRoles`, JSON.stringify([]));
      } catch {
        localStorage.removeItem(`${project}:token`);
        localStorage.removeItem(`${project}:user`);
        localStorage.removeItem(`${project}:userRoles`);
        localStorage.removeItem(`${project}:userName`);
      }
    }
    setLoading(false);
  }, []);

  const mapApiUser = (u, usernameFallback) => ({
    id: u.id,
    userName: u.userName ?? u.username ?? usernameFallback,
    email: u.email ?? '',
    active: u.active !== false,
    displayName: u.userName ?? u.username ?? usernameFallback ?? 'Usuário',
  });

  const signIn = async ({ username, password }) => {
    try {
      let accessToken;
      let userData;
      let userRoles;

      if (USE_MOCK_AUTH) {
        const mockUser = Object.values(MOCK_USERS).find((u) => u.username === username && u.password === password);
        if (!mockUser) {
          await new Promise((r) => setTimeout(r, 300));
          return {
            success: false,
            error: { response: { status: 401, data: { message: 'Usuário ou senha inválidos' } } },
          };
        }
        await new Promise((r) => setTimeout(r, 300));
        accessToken = mockUser.accessToken;
        userData = mockUser.user;
        userRoles = mockUser.roles;
      } else {
        const response = await Api.post('/api/SignIn/signin', {
          username,
          password,
        });
        const payload = response.data;
        if (!payload?.success) {
          return {
            success: false,
            error: {
              response: {
                status: 400,
                data: { message: payload?.message, errors: payload?.errors },
              },
            },
          };
        }
        const inner = payload.data;
        accessToken = inner.token;
        userData = mapApiUser(inner.user, username);
        userRoles = inner.roles || [];
      }

      localStorage.setItem(`${project}:token`, accessToken);
      localStorage.setItem(`${project}:user`, JSON.stringify(userData));
      localStorage.setItem(`${project}:userRoles`, JSON.stringify(userRoles));
      localStorage.setItem(`${project}:userName`, userData.displayName || userData.userName || username);

      setToken(accessToken);
      setUser(userData);

      return { success: true };
    } catch (error) {
      return { success: false, error };
    }
  };

  const register = async ({ userName, email, password }) => {
    try {
      if (USE_MOCK_AUTH) {
        return { success: false, error: { message: 'Registro disponível apenas com API real.' } };
      }
      const response = await Api.post('/api/SignIn/register', {
        userName,
        email,
        password,
      });
      const payload = response.data;
      if (!payload?.success) {
        return {
          success: false,
          error: {
            response: { status: 400, data: { message: payload?.message, errors: payload?.errors } },
          },
        };
      }
      const inner = payload.data;
      const accessToken = inner.token;
      const userData = mapApiUser(inner.user, userName);
      const userRoles = inner.roles || [];

      localStorage.setItem(`${project}:token`, accessToken);
      localStorage.setItem(`${project}:user`, JSON.stringify(userData));
      localStorage.setItem(`${project}:userRoles`, JSON.stringify(userRoles));
      localStorage.setItem(`${project}:userName`, userData.displayName || userName);

      setToken(accessToken);
      setUser(userData);

      return { success: true };
    } catch (error) {
      return { success: false, error };
    }
  };

  const signOut = () => {
    localStorage.removeItem(`${project}:token`);
    localStorage.removeItem(`${project}:user`);
    localStorage.removeItem(`${project}:userRoles`);
    localStorage.removeItem(`${project}:userName`);
    setToken(null);
    setUser(null);
  };

  const value = {
    user,
    token,
    loading,
    signIn,
    register,
    signOut,
    isAuthenticated: !!token && !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  return context;
};
