import { Checkbox } from 'antd';

/**
 * Componente base de grupo de checkboxes usando Ant Design
 */
const BaseCheckboxGroupInput = ({ ...props }) => {
  return <Checkbox.Group {...props} />;
};

export default BaseCheckboxGroupInput;
