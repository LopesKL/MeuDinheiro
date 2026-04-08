import { lazy } from 'react';

/**
 * Preload de componente lazy
 * @param {Function} lazyComponent - Componente lazy retornado por React.lazy()
 */
export const preloadComponent = (lazyComponent) => {
  if (lazyComponent && lazyComponent._payload && lazyComponent._payload._status === -1) {
    lazyComponent._payload._result();
  }
};

/**
 * Hook para criar componente lazy com preload
 * @param {Function} importFn - Função de importação do componente
 * @returns {Object} Componente lazy e função de preload
 */
export const useLazyComponent = (importFn) => {
  const LazyComponent = lazy(importFn);
  
  const preload = () => {
    preloadComponent(LazyComponent);
  };

  return { LazyComponent, preload };
};

export default useLazyComponent;
