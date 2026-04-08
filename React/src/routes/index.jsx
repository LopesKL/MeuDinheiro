import { createBrowserRouter } from 'react-router-dom';
import { lazy } from 'react';
import { defaultRoutes, generateRoutes } from './routes';
import RouteWrapper from './route';
import { LazyWrapper } from '@components/UI';

// Layout autorizado
const AuthorizedLayout = lazy(() => import('@pages/Layouts/Authorized'));

// Página de login
const SignIn = lazy(() => import('@pages/SignIn'));

// Página 404
const NotFound = lazy(() => import('@pages/Layouts/Authorized/notFound'));

/**
 * Gera todas as rotas da aplicação
 */
const generatedRoutes = generateRoutes(defaultRoutes, RouteWrapper);

const router = createBrowserRouter([
  {
    path: '/signIn',
    element: (
      <LazyWrapper>
        <SignIn />
      </LazyWrapper>
    ),
  },
  {
    path: '/',
    element: (
      <LazyWrapper>
        <AuthorizedLayout />
      </LazyWrapper>
    ),
    children: generatedRoutes,
  },
  {
    path: '*',
    element: (
      <LazyWrapper>
        <AuthorizedLayout>
          <NotFound />
        </AuthorizedLayout>
      </LazyWrapper>
    ),
  },
]);

export default router;
