import { Form } from 'antd';
import BaseTextInput from './BaseTextInput';

/**
 * Wrapper de input de email com validação automática
 */
const EmailInput = ({ name, label, rules, ...props }) => {
  const defaultRules = [
    {
      type: 'email',
      message: 'Por favor, insira um email válido',
    },
    ...(rules || []),
  ];

  return (
    <Form.Item name={name} label={label} rules={defaultRules}>
      <BaseTextInput type="email" {...props} />
    </Form.Item>
  );
};

export default EmailInput;
