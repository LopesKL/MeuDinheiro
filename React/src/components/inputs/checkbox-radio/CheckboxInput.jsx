import { Form } from 'antd';
import BaseCheckboxInput from './BaseCheckboxInput';

/**
 * Wrapper de checkbox com integração ao Form do Ant Design
 */
const CheckboxInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} valuePropName="checked" rules={rules}>
      <BaseCheckboxInput {...props}>{label}</BaseCheckboxInput>
    </Form.Item>
  );
};

export default CheckboxInput;
