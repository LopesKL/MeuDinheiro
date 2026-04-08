import { Form } from 'antd';
import BaseTimeInput from './BaseTimeInput';

/**
 * Wrapper de input de hora com integração ao Form do Ant Design
 */
const TimeInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseTimeInput {...props} />
    </Form.Item>
  );
};

export default TimeInput;
