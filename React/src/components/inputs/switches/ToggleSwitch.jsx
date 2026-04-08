import { Form } from 'antd';
import BaseToggleSwitchInput from './BaseToggleSwitchInput';

/**
 * Wrapper de toggle switch com integração ao Form do Ant Design
 */
const ToggleSwitch = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} valuePropName="checked" rules={rules}>
      <BaseToggleSwitchInput {...props} />
    </Form.Item>
  );
};

export default ToggleSwitch;
