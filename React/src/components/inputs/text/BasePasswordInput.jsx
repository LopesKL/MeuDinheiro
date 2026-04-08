import { Input } from 'antd';

/**
 * Componente base de input de senha usando Ant Design
 */
const BasePasswordInput = ({ ...props }) => {
  return <Input.Password {...props} />;
};

export default BasePasswordInput;
