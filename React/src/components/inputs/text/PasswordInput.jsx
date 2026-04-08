import { Form } from 'antd';
import BasePasswordInput from './BasePasswordInput';

/**
 * Wrapper de input de senha com integração ao Form do Ant Design
 */
const PasswordInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BasePasswordInput {...props} />
    </Form.Item>
  );
};

export default PasswordInput;
