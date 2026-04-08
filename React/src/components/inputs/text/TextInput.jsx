import { Form } from 'antd';
import BaseTextInput from './BaseTextInput';

/**
 * Wrapper de input de texto com integração ao Form do Ant Design
 */
const TextInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseTextInput {...props} />
    </Form.Item>
  );
};

export default TextInput;
