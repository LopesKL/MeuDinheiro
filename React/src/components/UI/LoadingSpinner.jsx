import { SyncOutlined } from '@ant-design/icons';
import { Spin } from 'antd';
import { colors } from '@styles/colors';

/**
 * Componente de spinner de loading customizado
 * @param {Object} props
 * @param {number} props.size - Tamanho do spinner (default: 24)
 * @param {string} props.color - Cor do spinner (default: colors.primary)
 * @param {Object} props.style - Estilos adicionais
 * @param {string} props.className - Classe CSS adicional
 */
const LoadingSpinner = ({ size = 24, color = colors.primary, style, className }) => {
  return (
    <Spin
      indicator={
        <SyncOutlined
          spin
          style={{
            fontSize: size,
            color,
          }}
        />
      }
      style={style}
      className={className}
    />
  );
};

export default LoadingSpinner;
