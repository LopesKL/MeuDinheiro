import { Form } from 'antd';
import BaseNumberInput from './BaseNumberInput';

/**
 * Wrapper de input numérico com integração ao Form do Ant Design
 */
const NumberInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseNumberInput {...props} />
    </Form.Item>
  );
};

export default NumberInput;
