import { Layout, Menu, Button, Avatar, Dropdown } from 'antd';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { useAuth, useThemeMode } from '@hooks';
import { defaultRoutes } from '@routes/routes';
import {
  LogoutOutlined,
  UserOutlined,
  MoonOutlined,
  SunOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons';
import { colors } from '@styles/colors';

const { Header, Sider, Content } = Layout;

const MOBILE_MQ = '(max-width: 767px)';

const SIDEBAR_EASE = 'cubic-bezier(0.4, 0, 0.2, 1)';
const SIDEBAR_DURATION = '0.28s';
const SIDEBAR_TRANSITION = [
  `width ${SIDEBAR_DURATION} ${SIDEBAR_EASE}`,
  `min-width ${SIDEBAR_DURATION} ${SIDEBAR_EASE}`,
  `max-width ${SIDEBAR_DURATION} ${SIDEBAR_EASE}`,
  `flex-basis ${SIDEBAR_DURATION} ${SIDEBAR_EASE}`,
].join(', ');

const AuthorizedLayout = ({ children }) => {
  const [isMobile, setIsMobile] = useState(() =>
    typeof window !== 'undefined' ? window.matchMedia(MOBILE_MQ).matches : false
  );

  const [collapsed, setCollapsed] = useState(() =>
    typeof window !== 'undefined' ? window.matchMedia(MOBILE_MQ).matches : false
  );

  useEffect(() => {
    const mq = window.matchMedia(MOBILE_MQ);
    const sync = () => {
      const mobile = mq.matches;
      setIsMobile(mobile);
      if (mobile) {
        setCollapsed(true);
      }
    };
    sync();
    mq.addEventListener('change', sync);
    return () => mq.removeEventListener('change', sync);
  }, []);
  const navigate = useNavigate();
  const location = useLocation();
  const { user, signOut } = useAuth();
  const { dark, toggleTheme } = useThemeMode();

  const userName = localStorage.getItem('framework:userName') || user?.name || 'Usuário';

  // Encontrar rota atual e abrir submenu se necessário
  const getSelectedKeys = () => {
    const path = location.pathname;
    return [path];
  };

  const getOpenKeys = () => {
    const path = location.pathname;
    const route = defaultRoutes.find((r) => r.key === path || (r.children && r.children.some((c) => c.key === path)));
    if (route && route.children) {
      return [route.key];
    }
    return [];
  };

  const handleMenuClick = ({ key }) => {
    if (isMobile) {
      setCollapsed(true);
    }
    if (key === location.pathname) {
      // Se clicar na mesma rota, recarregar a página
      window.location.reload();
    } else {
      navigate(key);
    }
  };

  const handleLogout = () => {
    signOut();
    navigate('/signIn');
  };

  const menuItems = defaultRoutes.map((route) => ({
    key: route.key,
    icon: route.icon,
    label: route.label,
  }));

  const userMenuItems = [
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Sair',
      onClick: handleLogout,
    },
  ];

  const contentMarginLeft = isMobile ? 0 : collapsed ? 80 : 200;

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {isMobile && !collapsed && (
        <div
          aria-hidden
          className="authorized-layout-overlay"
          onClick={() => setCollapsed(true)}
          style={{
            position: 'fixed',
            inset: 0,
            zIndex: 1000,
            background: 'rgba(0, 0, 0, 0.45)',
          }}
        />
      )}
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        collapsedWidth={isMobile ? 0 : 80}
        width={200}
        trigger={isMobile ? null : undefined}
        style={{
          overflow: 'auto',
          height: '100vh',
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: isMobile && !collapsed ? 1001 : undefined,
          transition: SIDEBAR_TRANSITION,
          ...(isMobile && collapsed ? { pointerEvents: 'none' } : {}),
        }}
      >
        <div
          style={{
            height: 64,
            margin: 16,
            background: colors.primary,
            borderRadius: 4,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: colors.white,
            fontWeight: 'bold',
          }}
        >
          {collapsed ? 'F' : 'Finanças'}
        </div>
        <Menu
          theme="dark"
          selectedKeys={getSelectedKeys()}
          defaultOpenKeys={getOpenKeys()}
          mode="inline"
          items={menuItems}
          onClick={handleMenuClick}
        />
      </Sider>
      <Layout
        style={{
          marginLeft: contentMarginLeft,
          transition: `margin-left ${SIDEBAR_DURATION} ${SIDEBAR_EASE}`,
        }}
      >
        <Header
          style={{
            position: 'sticky',
            top: 0,
            zIndex: 1002,
            width: '100%',
            display: 'flex',
            height: 64,
            alignItems: 'center',
            gap: 8,
            padding: '0 24px',
            background: dark ? '#141414' : colors.white,
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
          }}
        >
          {isMobile && (
            <Button
              type="text"
              aria-label={collapsed ? 'Abrir menu' : 'Fechar menu'}
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed((c) => !c)}
            />
          )}
          <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Button
              type="text"
              aria-label={dark ? 'Modo claro' : 'Modo escuro'}
              icon={dark ? <SunOutlined /> : <MoonOutlined />}
              onClick={toggleTheme}
            />
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <Button type="text" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <Avatar icon={<UserOutlined />} />
                <span>{userName}</span>
              </Button>
            </Dropdown>
          </div>
        </Header>
        <Content
          style={{
            margin: '24px 16px',
            padding: 0,
            minHeight: 280,
            background: "transparent",
            borderRadius: 8,
          }}
        >
          {children || <Outlet />}
        </Content>
      </Layout>
    </Layout>
  );
};

export default AuthorizedLayout;
