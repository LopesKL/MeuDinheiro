import { Form } from 'antd';
import BaseSwitchInput from './BaseSwitchInput';

/**
 * Wrapper de switch com integração ao Form do Ant Design
 */
const SwitchInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} valuePropName="checked" rules={rules}>
      <BaseSwitchInput {...props} />
    </Form.Item>
  );
};

export default SwitchInput;
