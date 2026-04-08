import { Form } from 'antd';
import BaseMaskedInput from './BaseMaskedInput';

/**
 * Wrapper de input de telefone com máscara
 */
const PhoneInput = ({ name, label, rules, ...props }) => {
  const defaultRules = [
    {
      validator: (_, value) => {
        if (!value) return Promise.resolve();
        const cleanValue = value.replace(/[^\d]/g, '');
        if (cleanValue.length !== 11) {
          return Promise.reject(new Error('Telefone deve ter 11 dígitos'));
        }
        return Promise.resolve();
      },
    },
    ...(rules || []),
  ];

  return (
    <Form.Item name={name} label={label} rules={defaultRules}>
      <BaseMaskedInput mask="(99) 99999-9999" {...props} />
    </Form.Item>
  );
};

export default PhoneInput;
