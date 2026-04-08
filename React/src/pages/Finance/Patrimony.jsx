import { useEffect, useState } from 'react';
import {
  Card,
  Table,
  Typography,
  Statistic,
  Row,
  Col,
  Button,
  Modal,
  Form,
  Input,
  InputNumber,
  Select,
  Radio,
  Space,
  Grid,
  Divider,
  Popconfirm,
  App,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { financeApi } from '@services/financeApi';

const { Title, Text } = Typography;

const emptyGuid = '00000000-0000-0000-0000-000000000000';

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

const accountTypes = ['Banco', 'Investimento', 'Cripto', 'Outro'];

const mobileCardSx = {
  background: '#fff',
  borderRadius: 12,
  border: '1px solid #ececec',
  padding: 16,
  marginBottom: 12,
  boxShadow: '0 1px 3px rgba(0, 0, 0, 0.06)',
};

function CrudMobileHeader({ count, addLabel, onAdd }) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 16,
        gap: 12,
        flexWrap: 'wrap',
      }}
    >
      <Text style={{ fontSize: 15 }}>
        {count} {count === 1 ? 'registro' : 'registros'}
      </Text>
      <Button type="primary" icon={<PlusOutlined />} onClick={onAdd} style={{ borderRadius: 8 }}>
        {addLabel}
      </Button>
    </div>
  );
}

function MobileCrudCard({ rows, onEdit, onDelete, deleteTitle, footer }) {
  return (
    <div style={mobileCardSx}>
      {rows.map((row, i) => (
        <div
          key={i}
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            gap: 12,
            marginBottom: i < rows.length - 1 ? 10 : 0,
          }}
        >
          <Text type="secondary" style={{ fontSize: 12, textTransform: 'uppercase', flexShrink: 0 }}>
            {row.label}
          </Text>
          <Text strong style={{ textAlign: 'right', wordBreak: 'break-word' }}>
            {row.value ?? '—'}
          </Text>
        </div>
      ))}
      <Divider style={{ margin: '14px 0 12px' }} />
      {footer ?? (
        <div style={{ display: 'flex', gap: 8 }}>
          <Button icon={<EditOutlined />} onClick={onEdit} style={{ flex: 1, borderRadius: 8 }}>
            Editar
          </Button>
          <div style={{ flex: 1 }}>
            <Popconfirm title={deleteTitle || 'Remover?'} onConfirm={onDelete}>
              <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                Excluir
              </Button>
            </Popconfirm>
          </div>
        </div>
      )}
    </div>
  );
}

const Patrimony = () => {
  const { message } = App.useApp();
  const screens = Grid.useBreakpoint();
  const isMobile = screens.md === false;
  const [form] = Form.useForm();
  const [adjustForm] = Form.useForm();
  const [accounts, setAccounts] = useState([]);
  const [total, setTotal] = useState(0);
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [accountModalMode, setAccountModalMode] = useState('create');
  const [saving, setSaving] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);
  const [adjustingAccount, setAdjustingAccount] = useState(null);
  const [adjustSaving, setAdjustSaving] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const [list, t, h] = await Promise.all([
        financeApi.accounts(),
        financeApi.accountsTotal(),
        financeApi.patrimonyHistory(12),
      ]);
      setAccounts(list || []);
      setTotal(Number(t) || 0);
      setHistory(
        (h || []).map((x) => ({
          name: x.label,
          total: Number(x.totalBalance),
        }))
      );
    } catch (e) {
      message.error(e.message || 'Erro ao carregar patrimônio');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [message]);

  const openAddModal = () => {
    setAccountModalMode('create');
    form.resetFields();
    form.setFieldsValue({ id: emptyGuid, type: 0, balance: 0, currency: 'BRL' });
    setModalOpen(true);
  };

  const openEditModal = (record) => {
    setAccountModalMode('edit');
    form.resetFields();
    form.setFieldsValue({
      id: record.id,
      name: record.name,
      type: record.type,
      balance: Number(record.balance ?? 0),
      currency: record.currency || 'BRL',
    });
    setModalOpen(true);
  };

  const openAdjustModal = (record) => {
    setAdjustingAccount(record);
    adjustForm.resetFields();
    adjustForm.setFieldsValue({ mode: 'inc', amount: undefined });
    setAdjustOpen(true);
  };

  const submitAccount = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await financeApi.accountUpsert(v);
      message.success(accountModalMode === 'create' ? 'Conta cadastrada' : 'Conta atualizada');
      setModalOpen(false);
      await load();
    } catch (e) {
      if (e?.errorFields) return;
      message.error(e.message || 'Erro ao salvar');
    } finally {
      setSaving(false);
    }
  };

  const submitAdjust = async () => {
    const v = await adjustForm.validateFields();
    if (!adjustingAccount) return;
    const cur = Number(adjustingAccount.balance ?? 0);
    let newBalance;
    if (v.mode === 'inc') newBalance = cur + Number(v.amount);
    else if (v.mode === 'dec') newBalance = cur - Number(v.amount);
    else newBalance = Number(v.amount);

    setAdjustSaving(true);
    try {
      await financeApi.accountUpsert({
        id: adjustingAccount.id,
        name: adjustingAccount.name,
        type: adjustingAccount.type,
        balance: newBalance,
        currency: adjustingAccount.currency || 'BRL',
      });
      message.success('Saldo atualizado');
      setAdjustOpen(false);
      setAdjustingAccount(null);
      await load();
    } catch (e) {
      if (e?.errorFields) return;
      message.error(e.message || 'Erro ao atualizar saldo');
    } finally {
      setAdjustSaving(false);
    }
  };

  const handleDeleteAccount = async (id) => {
    try {
      await financeApi.accountDelete(id);
      message.success('Conta removida');
      await load();
    } catch (e) {
      message.error(e.message || 'Erro ao remover');
    }
  };

  const columns = [
    { title: 'Conta', dataIndex: 'name', key: 'name' },
    {
      title: 'Tipo',
      dataIndex: 'type',
      key: 'type',
      render: (t) => accountTypes[t] ?? t,
    },
    { title: 'Saldo', dataIndex: 'balance', key: 'balance', render: (v) => formatMoney(v) },
    { title: 'Moeda', dataIndex: 'currency', key: 'currency' },
    {
      title: 'Ações',
      key: 'actions',
      render: (_, record) => (
        <Space wrap>
          <Button size="small" type="primary" onClick={() => openAdjustModal(record)}>
            Movimentar
          </Button>
          <Button size="small" onClick={() => openEditModal(record)}>
            Editar
          </Button>
          <Popconfirm title="Remover conta?" onConfirm={() => handleDeleteAccount(record.id)}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Patrimônio
      </Title>
      <Row gutter={[16, 16]}>
        <Col xs={24} md={8}>
          <Card loading={loading}>
            <Statistic title="Total em contas" value={total} formatter={formatMoney} />
          </Card>
        </Col>
      </Row>
      <Card title="Evolução mensal (snapshots)" style={{ marginTop: 16 }} loading={loading}>
        <div style={{ width: '100%', height: 300 }}>
          <ResponsiveContainer>
            <LineChart data={history}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" />
              <YAxis />
              <Tooltip formatter={(v) => formatMoney(v)} />
              <Line type="monotone" dataKey="total" stroke="#1677ff" dot />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </Card>
      <Card
        title="Onde está seu dinheiro"
        style={{ marginTop: 16 }}
        loading={loading}
        extra={
          !isMobile ? (
            <Button type="primary" onClick={openAddModal}>
              Nova conta
            </Button>
          ) : null
        }
      >
        {!isMobile && (
          <Typography.Paragraph type="secondary" style={{ marginTop: 0 }}>
            Cada linha é um lugar (banco, investimento, etc.). O saldo é o valor <strong>hoje</strong> — use
            &quot;Movimentar&quot; para entradas, saídas ou definir o saldo exato.
          </Typography.Paragraph>
        )}
        {isMobile ? (
          <>
            <CrudMobileHeader count={accounts.length} addLabel="Nova conta" onAdd={openAddModal} />
            {accounts.map((r) => (
              <MobileCrudCard
                key={r.id}
                rows={[
                  { label: 'Conta', value: r.name },
                  { label: 'Tipo', value: accountTypes[r.type] ?? r.type },
                  { label: 'Saldo', value: formatMoney(r.balance) },
                  { label: 'Moeda', value: r.currency || 'BRL' },
                ]}
                footer={
                  <div>
                    <Button
                      type="primary"
                      block
                      onClick={() => openAdjustModal(r)}
                      style={{ marginBottom: 8, borderRadius: 8 }}
                    >
                      Movimentar
                    </Button>
                    <div style={{ display: 'flex', gap: 8 }}>
                      <Button
                        icon={<EditOutlined />}
                        onClick={() => openEditModal(r)}
                        style={{ flex: 1, borderRadius: 8 }}
                      >
                        Editar
                      </Button>
                      <div style={{ flex: 1 }}>
                        <Popconfirm title="Remover conta?" onConfirm={() => handleDeleteAccount(r.id)}>
                          <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                            Excluir
                          </Button>
                        </Popconfirm>
                      </div>
                    </div>
                  </div>
                }
              />
            ))}
            {accounts.length === 0 && <Text type="secondary">Nenhuma conta cadastrada.</Text>}
          </>
        ) : (
          <Table rowKey="id" columns={columns} dataSource={accounts} pagination={false} scroll={{ x: true }} />
        )}
      </Card>

      <Modal
        title={accountModalMode === 'create' ? 'Nova conta' : 'Editar conta'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={submitAccount}
        confirmLoading={saving}
        destroyOnClose
        okText="Salvar"
      >
        <Form form={form} layout="vertical">
          <Form.Item name="id" hidden>
            <Input />
          </Form.Item>
          <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
            <Input placeholder="Ex.: Nubank, corretora..." />
          </Form.Item>
          <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 0, label: 'Banco' },
                { value: 1, label: 'Investimento' },
                { value: 2, label: 'Cripto' },
                { value: 3, label: 'Outro' },
              ]}
            />
          </Form.Item>
          <Form.Item
            name="balance"
            label={accountModalMode === 'create' ? 'Saldo inicial' : 'Saldo atual'}
            rules={[{ required: true }]}
            extra={
              accountModalMode === 'create'
                ? 'Pode começar em zero e usar Movimentar depois.'
                : 'Ou use só Movimentar na lista para somar/subtrair sem editar aqui.'
            }
          >
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="currency" label="Moeda">
            <Input />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={adjustingAccount ? `Movimentar — ${adjustingAccount.name}` : 'Movimentar'}
        open={adjustOpen}
        onCancel={() => {
          setAdjustOpen(false);
          setAdjustingAccount(null);
        }}
        onOk={submitAdjust}
        confirmLoading={adjustSaving}
        destroyOnClose
        okText="Aplicar"
      >
        {adjustingAccount && (
          <Typography.Paragraph>
            Saldo atual: <strong>{formatMoney(adjustingAccount.balance)}</strong>
          </Typography.Paragraph>
        )}
        <Form form={adjustForm} layout="vertical">
          <Form.Item name="mode" label="O que fazer?" rules={[{ required: true }]}>
            <Radio.Group>
              <Radio.Button value="inc">Entrada (+)</Radio.Button>
              <Radio.Button value="dec">Saída (−)</Radio.Button>
              <Radio.Button value="set">Definir saldo exato</Radio.Button>
            </Radio.Group>
          </Form.Item>
          <Form.Item
            noStyle
            shouldUpdate={(prev, cur) => prev.mode !== cur.mode}
          >
            {({ getFieldValue }) => {
              const mode = getFieldValue('mode');
              const isSet = mode === 'set';
              return (
                <Form.Item
                  name="amount"
                  label={isSet ? 'Novo saldo na conta' : 'Valor'}
                  rules={[
                    { required: true, message: 'Informe o valor' },
                    ...(!isSet
                      ? [
                          {
                            type: 'number',
                            min: 0.01,
                            message: 'Use um valor maior que zero',
                          },
                        ]
                      : []),
                  ]}
                >
                  <InputNumber style={{ width: '100%' }} min={isSet ? undefined : 0.01} step={0.01} />
                </Form.Item>
              );
            }}
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Patrimony;
