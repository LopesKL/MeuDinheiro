import { Radio } from 'antd';

/**
 * Componente base de radio usando Ant Design
 */
const BaseRadioInput = ({ ...props }) => {
  return <Radio.Group {...props} />;
};

export default BaseRadioInput;
