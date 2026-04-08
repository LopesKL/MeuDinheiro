import { Form } from 'antd';
import BaseNumberInput from './BaseNumberInput';

/**
 * Wrapper de input de moeda com formatação
 */
const CurrencyInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseNumberInput
        formatter={(value) => {
          if (!value) return '';
          return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL',
          }).format(value);
        }}
        parser={(value) => {
          if (!value) return '';
          return value.replace(/[^\d,.-]/g, '').replace(',', '.');
        }}
        {...props}
      />
    </Form.Item>
  );
};

export default CurrencyInput;
