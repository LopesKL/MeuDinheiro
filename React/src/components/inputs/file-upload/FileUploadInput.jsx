import { Form, Upload } from 'antd';
import { UploadOutlined } from '@ant-design/icons';

/**
 * Wrapper de upload de arquivo com integração ao Form do Ant Design
 */
const FileUploadInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules} valuePropName="fileList" getValueFromEvent={(e) => (Array.isArray(e) ? e : e?.fileList)}>
      <Upload {...props}>
        <button type="button" style={{ border: 0, background: 'none' }}>
          <UploadOutlined /> Clique para fazer upload
        </button>
      </Upload>
    </Form.Item>
  );
};

export default FileUploadInput;
