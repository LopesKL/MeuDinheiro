import { Card as AntCard } from 'antd';
import { colors } from '@styles/colors';

/**
 * Wrapper customizado do Card do Ant Design
 * @param {Object} props
 * @param {string} props.variant - Variante: 'default' | 'borderless' | 'outlined'
 * @param {string} props.size - Tamanho: 'small' | 'default' | 'large'
 * @param {boolean} props.hoverable - Se tem efeito hover
 * @param {React.ReactNode} props.icon - Ícone no título
 * @param {string} props.description - Descrição no título
 * @param {React.ReactNode} props.extra - Conteúdo extra no header
 * @param {React.ReactNode} props.footer - Footer do card
 */
const Card = ({
  variant = 'default',
  size = 'default',
  hoverable = false,
  icon,
  description,
  extra,
  footer,
  title,
  children,
  ...rest
}) => {
  const getBordered = () => {
    if (variant === 'borderless') return false;
    if (variant === 'outlined') return true;
    return true; // default
  };

  const cardTitle = (
    <div>
      {icon && <span style={{ marginRight: 8 }}>{icon}</span>}
      {title}
      {description && (
        <div style={{ fontSize: '12px', color: colors.text.secondary, marginTop: 4 }}>
          {description}
        </div>
      )}
    </div>
  );

  return (
    <AntCard
      title={cardTitle}
      extra={extra}
      bordered={getBordered()}
      hoverable={hoverable}
      size={size}
      style={{
        ...(variant === 'outlined' && {
          border: `1px solid ${colors.text.secondary}`,
        }),
        ...rest.style,
      }}
      {...rest}
    >
      {children}
      {footer && <div style={{ marginTop: 16, paddingTop: 16, borderTop: `1px solid ${colors.text.secondary}` }}>{footer}</div>}
    </AntCard>
  );
};

export default Card;
