import { Form } from 'antd';
import BaseDateRangeInput from './BaseDateRangeInput';

/**
 * Wrapper de input de range de datas com integração ao Form do Ant Design
 */
const DateRangeInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseDateRangeInput {...props} />
    </Form.Item>
  );
};

export default DateRangeInput;
