import { Form } from 'antd';
import BaseTextAreaInput from './BaseTextAreaInput';

/**
 * Wrapper de textarea com integração ao Form do Ant Design
 */
const TextAreaInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseTextAreaInput {...props} />
    </Form.Item>
  );
};

export default TextAreaInput;
