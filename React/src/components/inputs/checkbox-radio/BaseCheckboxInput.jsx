import { Checkbox } from 'antd';

/**
 * Componente base de checkbox usando Ant Design
 */
const BaseCheckboxInput = ({ ...props }) => {
  return <Checkbox {...props} />;
};

export default BaseCheckboxInput;
