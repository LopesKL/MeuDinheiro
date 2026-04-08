import { Form } from 'antd';
import BaseMaskedInput from './BaseMaskedInput';
import { validateCNPJ } from '@helpers/helper';

/**
 * Wrapper de input de CNPJ com máscara e validação
 */
const CnpjInput = ({ name, label, rules, ...props }) => {
  const defaultRules = [
    {
      validator: (_, value) => {
        if (!value) return Promise.resolve();
        const cleanValue = value.replace(/[^\d]/g, '');
        if (cleanValue.length === 14 && !validateCNPJ(cleanValue)) {
          return Promise.reject(new Error('CNPJ inválido'));
        }
        return Promise.resolve();
      },
    },
    ...(rules || []),
  ];

  return (
    <Form.Item name={name} label={label} rules={defaultRules}>
      <BaseMaskedInput mask="99.999.999/9999-99" {...props} />
    </Form.Item>
  );
};

export default CnpjInput;
