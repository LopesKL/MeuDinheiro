import { useEffect, useState } from 'react';
import { Card, Upload, Typography, Descriptions, Button, Form, InputNumber, Select, DatePicker, Input } from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { financeApi, EXPENSE_CREATION_SOURCE } from '@services/financeApi';
import { useAuth } from '@hooks';
import { App } from 'antd';

const { Title, Text } = Typography;
const { Dragger } = Upload;

const paymentOptions = [
  { value: 0, label: 'Dinheiro' },
  { value: 1, label: 'Débito' },
  { value: 2, label: 'Crédito' },
  { value: 3, label: 'Pix' },
  { value: 4, label: 'Transferência' },
  { value: 5, label: 'Outro' },
];

const UploadReceipt = () => {
  const { message } = App.useApp();
  const { token } = useAuth();
  const [ocr, setOcr] = useState(null);
  const [imagePath, setImagePath] = useState('');
  const [categories, setCategories] = useState([]);
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const loadCats = async () => {
    try {
      const c = await financeApi.categories();
      setCategories((c || []).filter((x) => x.isExpense));
    } catch {
      /* ignore */
    }
  };

  useEffect(() => {
    loadCats();
  }, []);

  const uploadProps = {
    name: 'file',
    multiple: false,
    showUploadList: false,
    customRequest: async ({ file, onSuccess, onError }) => {
      const fd = new FormData();
      fd.append('file', file);
      setLoading(true);
      try {
        const res = await financeApi.uploadReceipt(fd);
        setImagePath(res.imagePath);
        setOcr(res.ocr);
        form.setFieldsValue({
          amount: res.ocr?.detectedAmount,
          description: res.ocr?.merchantName || '',
          date: dayjs(),
          paymentMethod: 5,
        });
        message.success('Upload e OCR simulado concluídos');
        onSuccess?.('ok');
      } catch (e) {
        message.error(e.message || 'Falha no upload');
        onError?.(e);
      } finally {
        setLoading(false);
      }
    },
  };

  const onSave = async (values) => {
    setLoading(true);
    try {
      await financeApi.expenseUpsert({
        id: '00000000-0000-0000-0000-000000000000',
        amount: values.amount,
        date: values.date?.toDate?.().toISOString() || new Date().toISOString(),
        categoryId: values.categoryId,
        description: values.description || 'Comprovante',
        paymentMethod: values.paymentMethod ?? 5,
        storeLocation: null,
        creditCardId: null,
        installmentPlanId: null,
        imagePath: imagePath || null,
        creationSource: EXPENSE_CREATION_SOURCE.UPLOAD_RECEIPT,
      });
      message.success('Gasto salvo com anexo');
      form.resetFields();
      setOcr(null);
      setImagePath('');
    } catch (e) {
      message.error(e.message || 'Erro ao salvar');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Upload de comprovante
      </Title>
      <Card>
        {!token && <Text type="danger">Faça login para enviar arquivos.</Text>}
        <Dragger {...uploadProps} disabled={!token}>
          <p className="ant-upload-drag-icon">
            <InboxOutlined />
          </p>
          <p className="ant-upload-text">Clique ou arraste o print / foto da nota</p>
          <p className="ant-upload-hint">O backend aplica OCR simulado e devolve valor e estabelecimento sugeridos.</p>
        </Dragger>
        {ocr && (
          <Descriptions bordered size="small" style={{ marginTop: 16 }} column={1}>
            <Descriptions.Item label="Valor sugerido">{ocr.detectedAmount}</Descriptions.Item>
            <Descriptions.Item label="Estabelecimento">{ocr.merchantName}</Descriptions.Item>
            <Descriptions.Item label="Dica bruta">{ocr.rawHint}</Descriptions.Item>
          </Descriptions>
        )}
        <Title level={5} style={{ marginTop: 24 }}>
          Confirmar lançamento
        </Title>
        <Form form={form} layout="vertical" onFinish={onSave} initialValues={{ date: dayjs(), paymentMethod: 5 }}>
          <Form.Item name="amount" label="Valor" rules={[{ required: true }]}>
            <InputNumber min={0.01} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="date" label="Data" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="categoryId" label="Categoria" rules={[{ required: true }]}>
            <Select options={categories.map((c) => ({ value: c.id, label: c.name }))} showSearch optionFilterProp="label" />
          </Form.Item>
          <Form.Item name="description" label="Descrição">
            <Input />
          </Form.Item>
          <Form.Item name="paymentMethod" label="Forma de pagamento">
            <Select options={paymentOptions} />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={loading} disabled={!token}>
            Salvar gasto
          </Button>
        </Form>
      </Card>
    </div>
  );
};

export default UploadReceipt;
