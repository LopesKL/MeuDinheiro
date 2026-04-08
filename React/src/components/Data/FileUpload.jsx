import { Upload, Button } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import { useState } from 'react';

/**
 * Componente de upload de arquivo
 * @param {Object} props
 * @param {Function} props.onUpload - Função chamada ao fazer upload
 * @param {string} props.accept - Tipos de arquivo aceitos
 * @param {number} props.maxSize - Tamanho máximo em bytes
 * @param {number} props.maxCount - Número máximo de arquivos
 */
const FileUpload = ({ onUpload, accept, maxSize, maxCount = 1, ...props }) => {
  const [fileList, setFileList] = useState([]);

  const handleChange = (info) => {
    let newFileList = [...info.fileList];

    // Limitar número de arquivos
    if (maxCount) {
      newFileList = newFileList.slice(-maxCount);
    }

    // Validar tamanho
    if (maxSize) {
      newFileList = newFileList.filter((file) => {
        if (file.size) {
          return file.size <= maxSize;
        }
        return true;
      });
    }

    setFileList(newFileList);

    // Chamar callback quando upload completo
    if (info.file.status === 'done' && onUpload) {
      onUpload(newFileList);
    }
  };

  return (
    <Upload
      fileList={fileList}
      onChange={handleChange}
      accept={accept}
      {...props}
    >
      <Button icon={<UploadOutlined />}>Selecionar Arquivo</Button>
    </Upload>
  );
};

export default FileUpload;
