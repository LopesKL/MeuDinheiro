import { Input } from 'antd';

/**
 * Componente base de input de texto usando Ant Design
 */
const BaseTextInput = ({ ...props }) => {
  return <Input {...props} />;
};

export default BaseTextInput;
