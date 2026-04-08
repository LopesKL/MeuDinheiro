import { InputNumber } from 'antd';

/**
 * Componente base de input numérico usando Ant Design
 */
const BaseNumberInput = ({ ...props }) => {
  return <InputNumber {...props} style={{ width: '100%' }} />;
};

export default BaseNumberInput;
