import { lazy } from 'react';
import {
  DashboardOutlined,
  ThunderboltOutlined,
  CreditCardOutlined,
  BankOutlined,
  ExperimentOutlined,
  UploadOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';

export const defaultRoutes = [
  {
    key: '/',
    icon: <DashboardOutlined />,
    label: 'Dashboard',
    element: lazy(() => import('@pages/Finance/Dashboard')),
    roles: [],
  },
  {
    key: '/lancamento',
    icon: <ThunderboltOutlined />,
    label: 'Lançamento rápido',
    element: lazy(() => import('@pages/Finance/QuickLaunch')),
    roles: [],
  },

  {
    key: '/patrimonio',
    icon: <BankOutlined />,
    label: 'Patrimônio',
    element: lazy(() => import('@pages/Finance/Patrimony')),
    roles: [],
  },

  {
    key: '/cadastros',
    icon: <UnorderedListOutlined />,
    label: 'Cadastros',
    element: lazy(() => import('@pages/Finance/MasterData')),
    roles: [],
  },
];

export const generateRoutes = (routes, RouteWrapper) => {
  const result = [];
  routes.forEach((route) => {
    if (route.children && route.children.length > 0) {
      route.children.forEach((child) => {
        result.push({
          path: child.key,
          element: <RouteWrapper {...child} />,
        });
      });
    } else if (route.key === '/') {
      result.push({
        index: true,
        element: <RouteWrapper {...route} />,
      });
    } else {
      result.push({
        path: route.key.replace(/^\//, ''),
        element: <RouteWrapper {...route} />,
      });
    }
  });
  return result;
};
