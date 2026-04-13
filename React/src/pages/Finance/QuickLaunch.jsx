import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Card, Form, Input, Button, Select, DatePicker, InputNumber, Typography, Divider } from 'antd';
import dayjs from 'dayjs';
import { financeApi, EXPENSE_CREATION_SOURCE } from '@services/financeApi';
import { App } from 'antd';

const { Title, Text } = Typography;

const STORAGE_CARD = 'framework:selectedCreditCardId';

const paymentOptions = [
  { value: 0, label: 'Dinheiro' },
  { value: 1, label: 'Débito' },
  { value: 2, label: 'Crédito' },
  { value: 3, label: 'Pix' },
  { value: 4, label: 'Transferência' },
  { value: 5, label: 'Outro' },
];

const QuickLaunch = () => {
  const { message } = App.useApp();
  const [searchParams] = useSearchParams();
  const [form] = Form.useForm();
  const [parseForm] = Form.useForm();
  const [categories, setCategories] = useState([]);
  const [cards, setCards] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [cats, cc] = await Promise.all([financeApi.categories(), financeApi.creditCards()]);
        if (cancelled) return;
        setCategories((cats || []).filter((c) => c.isExpense));
        const list = cc || [];
        setCards(list);
        const fromUrl = searchParams.get('cartao');
        const fromStorage = sessionStorage.getItem(STORAGE_CARD);
        const id = fromUrl || fromStorage;
        if (id && list.some((c) => c.id === id)) {
          form.setFieldsValue({ creditCardId: id, paymentMethod: 2 });
          sessionStorage.setItem(STORAGE_CARD, id);
        }
      } catch (e) {
        if (!cancelled) message.error(e.message || 'Erro ao carregar categorias e cartões');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [searchParams, form, message]);

  const onParse = async () => {
    const text = parseForm.getFieldValue('smart');
    if (!text?.trim()) return;
    setLoading(true);
    try {
      const r = await financeApi.expenseParse(text);
      form.setFieldsValue({
        amount: r.amount ?? undefined,
        description: r.description || '',
        paymentMethod: r.paymentMethod ?? 5,
        categoryId: r.suggestedCategoryId,
        date: dayjs(),
      });
      if (r.suggestedCategoryName) message.success(`Sugestão: ${r.suggestedCategoryName}`);
    } catch (e) {
      message.error(e.message || 'Falha ao interpretar texto');
    } finally {
      setLoading(false);
    }
  };

  const onSave = async (values) => {
    setLoading(true);
    try {
      const paymentMethod = values.creditCardId ? 2 : (values.paymentMethod ?? 5);
      await financeApi.expenseUpsert({
        id: '00000000-0000-0000-0000-000000000000',
        amount: values.amount,
        date: values.date?.toDate?.() ? values.date.toDate().toISOString() : new Date().toISOString(),
        categoryId: values.categoryId,
        description: values.description || 'Gasto',
        paymentMethod,
        storeLocation: null,
        creditCardId: values.creditCardId || null,
        installmentPlanId: null,
        imagePath: null,
        creationSource: EXPENSE_CREATION_SOURCE.QUICK_LAUNCH,
      });
      message.success('Gasto registrado');
      form.resetFields();
      parseForm.resetFields();
      const fromUrl = searchParams.get('cartao');
      const fromStorage = sessionStorage.getItem(STORAGE_CARD);
      const keepId = fromUrl || fromStorage;
      const hasCard = keepId && cards.some((c) => c.id === keepId);
      form.setFieldsValue({
        date: dayjs(),
        paymentMethod: hasCard ? 2 : 5,
        creditCardId: hasCard ? keepId : undefined,
      });
    } catch (e) {
      message.error(e.message || 'Erro ao salvar');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: 0 }}>
      <Title level={3} style={{ marginTop: 0 }}>
        Lançamento rápido
      </Title>
      <Card style={{ marginBottom: 16 }}>
        <Text type="secondary">Ex.: &quot;50 mercado crédito&quot;</Text>
        <Form form={parseForm} layout="inline" style={{ marginTop: 12 }} onFinish={onParse}>
          <Form.Item name="smart" style={{ flex: 1, minWidth: 200,width: '100%'  }}>
            <Input placeholder="Valor + descrição + forma (opcional)" style={{ width: '100%' }} />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={loading} style={{ marginTop: 8, width: '100%' }}>
            Interpretar
          </Button>
        </Form>
      </Card>
      <Card
        title="Formulário"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={onSave}
          initialValues={{ date: dayjs(), paymentMethod: 5 }}
        >
          <Form.Item
            name="amount"
            label="Valor"
            style={{ width: '100%' }}
            rules={[
              { required: true, message: 'Informe o valor' },
              { type: 'number', min: 0.01, message: 'Informe um valor maior que zero' },
            ]}
          >
            <InputNumber
              prefix="R$"
              min={0.01}
              step={0.01}
              precision={2}
              decimalSeparator=","
              controls={false}
              placeholder="0,00"
              style={{ width: '100%' }}
            />
          </Form.Item>
          <Form.Item name="date" label="Data" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item
            name="creditCardId"
            label="Cartão cadastrado (opcional)"
          >
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Selecione um cartão ou deixe em branco"
              options={cards.map((c) => ({ value: c.id, label: c.name }))}
              onChange={(v) => {
                if (v) form.setFieldsValue({ paymentMethod: 2 });
              }}
            />
          </Form.Item>
          <Form.Item name="categoryId" label="Categoria" rules={[{ required: false }]}>
            <Select
              showSearch
              optionFilterProp="label"
              options={categories.map((c) => ({ value: c.id, label: c.name }))}
              placeholder="Selecione"
            />
          </Form.Item>
          <Form.Item name="description" label="Descrição">
            <Input />
          </Form.Item>
          <Divider />
          <div style={{ display: 'flex', gap: 8, width: '100%' }}>
            <Button onClick={() => form.resetFields()} style={{ flex: 1 }}>
              Limpar
            </Button>
            <Button type="primary" htmlType="submit" loading={loading} style={{ flex: 1 }}>
              Salvar gasto
            </Button>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default QuickLaunch;
