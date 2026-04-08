import { Form } from 'antd';
import BaseCheckboxGroupInput from './BaseCheckboxGroupInput';

/**
 * Wrapper de grupo de checkboxes com integração ao Form do Ant Design
 */
const CheckboxGroupInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseCheckboxGroupInput {...props} />
    </Form.Item>
  );
};

export default CheckboxGroupInput;
