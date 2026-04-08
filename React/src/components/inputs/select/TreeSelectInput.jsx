import { Form } from 'antd';
import BaseTreeSelectInput from './BaseTreeSelectInput';

/**
 * Wrapper de tree select com integração ao Form do Ant Design
 */
const TreeSelectInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseTreeSelectInput {...props} />
    </Form.Item>
  );
};

export default TreeSelectInput;
