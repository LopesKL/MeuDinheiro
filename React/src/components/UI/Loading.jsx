import { Skeleton, Spin } from 'antd';
import LoadingSpinner from './LoadingSpinner';

/**
 * Componente de loading com skeleton ou spinner
 * @param {Object} props
 * @param {boolean} props.small - Se true, mostra spinner pequeno
 * @param {boolean} props.useSkeleton - Se true, usa Skeleton ao invés de spinner
 * @param {string} props.message - Mensagem a ser exibida
 */
const Loading = ({ small = false, useSkeleton = false, message }) => {
  if (useSkeleton) {
    return <Skeleton active />;
  }

  if (small) {
    return <LoadingSpinner size={16} />;
  }

  return (
    <div style={{ textAlign: 'center', padding: '40px' }}>
      <Spin size="large" />
      {message && <div style={{ marginTop: 16 }}>{message}</div>}
    </div>
  );
};

export default Loading;
