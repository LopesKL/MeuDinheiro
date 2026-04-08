import { Suspense } from 'react';
import { Spin } from 'antd';

/**
 * Wrapper para lazy loading com Suspense
 * @param {Object} props
 * @param {React.ReactNode} props.children - Componente lazy a ser renderizado
 * @param {React.ReactNode} props.fallback - Fallback customizado (default: Spin)
 */
const LazyWrapper = ({ children, fallback }) => {
  const defaultFallback = (
    <div style={{ textAlign: 'center', padding: '40px' }}>
      <Spin size="large" />
    </div>
  );

  return (
    <Suspense fallback={fallback || defaultFallback}>
      {children}
    </Suspense>
  );
};

export default LazyWrapper;
