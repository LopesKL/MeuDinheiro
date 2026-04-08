import { Modal as AntModal, Button } from 'antd';
import { CheckOutlined, CloseOutlined } from '@ant-design/icons';

/**
 * Wrapper simplificado do Modal do Ant Design
 * @param {Object} props
 * @param {string} props.title - Título do modal
 * @param {React.ReactNode} props.content - Conteúdo do modal
 * @param {boolean} props.open - Se o modal está aberto
 * @param {Function} props.confirmFunction - Função a ser executada ao confirmar
 * @param {string} props.confirmButtonText - Texto do botão de confirmação
 * @param {Function} props.onCancel - Função a ser executada ao cancelar
 * @param {boolean} props.loading - Se está em estado de loading
 */
const Modal = ({
  title,
  content,
  open,
  confirmFunction,
  confirmButtonText = 'Confirmar',
  onCancel,
  loading = false,
  ...rest
}) => {
  return (
    <AntModal
      title={title}
      open={open}
      onCancel={onCancel}
      footer={[
        <Button
          key="cancel"
          icon={<CloseOutlined />}
          onClick={onCancel}
          disabled={loading}
        >
          Cancelar
        </Button>,
        <Button
          key="confirm"
          type="primary"
          icon={<CheckOutlined />}
          onClick={confirmFunction}
          loading={loading}
        >
          {confirmButtonText}
        </Button>,
      ]}
      {...rest}
    >
      {content}
    </AntModal>
  );
};

export default Modal;
