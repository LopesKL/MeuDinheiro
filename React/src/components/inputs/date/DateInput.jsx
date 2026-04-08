import { Form } from 'antd';
import BaseDateInput from './BaseDateInput';

/**
 * Wrapper de input de data com integração ao Form do Ant Design
 */
const DateInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseDateInput {...props} />
    </Form.Item>
  );
};

export default DateInput;
