import { Input } from 'antd';

/**
 * Componente base de textarea usando Ant Design
 */
const BaseTextAreaInput = ({ ...props }) => {
  return <Input.TextArea {...props} />;
};

export default BaseTextAreaInput;
