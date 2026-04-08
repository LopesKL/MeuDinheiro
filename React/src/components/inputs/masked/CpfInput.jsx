import { Form } from 'antd';
import BaseMaskedInput from './BaseMaskedInput';
import { validateCPF } from '@helpers/helper';

/**
 * Wrapper de input de CPF com máscara e validação
 */
const CpfInput = ({ name, label, rules, ...props }) => {
  const defaultRules = [
    {
      validator: (_, value) => {
        if (!value) return Promise.resolve();
        const cleanValue = value.replace(/[^\d]/g, '');
        if (cleanValue.length === 11 && !validateCPF(cleanValue)) {
          return Promise.reject(new Error('CPF inválido'));
        }
        return Promise.resolve();
      },
    },
    ...(rules || []),
  ];

  return (
    <Form.Item name={name} label={label} rules={defaultRules}>
      <BaseMaskedInput mask="999.999.999-99" {...props} />
    </Form.Item>
  );
};

export default CpfInput;
