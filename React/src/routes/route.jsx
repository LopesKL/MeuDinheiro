import { Navigate } from 'react-router-dom';
import { useAuth } from '@hooks';
import { LazyWrapper } from '@components/UI';
import { useEffect } from 'react';

/**
 * Wrapper de rota que verifica autenticação e roles
 * @param {Object} props
 * @param {boolean} props.auth - Se a rota requer autenticação
 * @param {Array} props.roles - Roles permitidos para a rota
 * @param {React.ComponentType} props.element - Componente lazy da rota
 */
const RouteWrapper = ({ auth = true, roles = [], element: Component }) => {
  const { isAuthenticated, user, loading } = useAuth();

  useEffect(() => {
    // Scroll para o topo ao mudar de rota
    window.scrollTo(0, 0);
  }, []);

  if (loading) {
    return <div>Carregando...</div>;
  }

  // Verificar autenticação
  if (auth && !isAuthenticated) {
    return <Navigate to="/signIn" replace />;
  }

  // Verificar roles
  if (auth && roles.length > 0 && user) {
    const userRoles = JSON.parse(localStorage.getItem('framework:userRoles') || '[]');
    const hasRole = userRoles.some((role) => roles.includes(role.name || role));
    
    if (!hasRole) {
      return <Navigate to="/" replace />;
    }
  }

  return (
    <LazyWrapper>
      <Component />
    </LazyWrapper>
  );
};

export default RouteWrapper;
